using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Ollama.Net.Exceptions;

namespace Ollama.Net.Http;

/// <summary>
/// Post-DNS IP-family enforcement used by the <c>SocketsHttpHandler.ConnectCallback</c>
/// wired up when <see cref="Configuration.OllamaClientOptions.DisallowPrivateNetworks"/>
/// is <see langword="true"/>. Rejects connections whose resolved IP sits in a
/// non-globally-routable range (RFC1918, link-local, unique-local, CGNAT, multicast,
/// broadcast, and — unless explicitly allowed — loopback).
/// </summary>
/// <remarks>
/// The callback is invoked for every DNS-resolved endpoint, including each redirect
/// hop, so a host that resolves to <c>8.8.8.8</c> today but <c>10.0.0.1</c> tomorrow
/// (or via a 302 to <c>http://internal/</c>) still gets refused.
/// </remarks>
internal static class PrivateNetworkGuard
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="address"/> is safe to dial
    /// from a hardened client that should never reach internal infrastructure.
    /// </summary>
    /// <param name="address">The DNS-resolved IP address.</param>
    /// <param name="allowLoopback">
    /// When <see langword="true"/>, loopback addresses (<c>127.0.0.0/8</c>, <c>::1</c>)
    /// are permitted. Enable this only when the configured base-address host is itself
    /// a loopback name so a developer-mode setup keeps working.
    /// </param>
    public static bool IsGloballyRoutable(IPAddress address, bool allowLoopback)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (IPAddress.IsLoopback(address))
        {
            return allowLoopback;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            IPAddress v4Mapped;
            if (address.IsIPv4MappedToIPv6)
            {
                v4Mapped = address.MapToIPv4();
                return IsGloballyRoutableIPv4(v4Mapped, allowLoopback);
            }

            return IsGloballyRoutableIPv6(address);
        }

        return IsGloballyRoutableIPv4(address, allowLoopback);
    }

    private static bool IsGloballyRoutableIPv4(IPAddress address, bool allowLoopback)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (!address.TryWriteBytes(bytes, out _))
        {
            return false;
        }

        byte a = bytes[0], b = bytes[1];

        // 0.0.0.0/8 — "this network"
        if (a == 0) return false;
        // 10.0.0.0/8 — RFC1918
        if (a == 10) return false;
        // 127.0.0.0/8 — loopback
        if (a == 127) return allowLoopback;
        // 169.254.0.0/16 — link-local
        if (a == 169 && b == 254) return false;
        // 172.16.0.0/12 — RFC1918
        if (a == 172 && (b & 0xF0) == 16) return false;
        // 192.0.0.0/24 — IETF protocol assignments
        if (a == 192 && b == 0 && bytes[2] == 0) return false;
        // 192.0.2.0/24 — TEST-NET-1
        if (a == 192 && b == 0 && bytes[2] == 2) return false;
        // 192.168.0.0/16 — RFC1918
        if (a == 192 && b == 168) return false;
        // 198.18.0.0/15 — benchmark
        if (a == 198 && (b & 0xFE) == 18) return false;
        // 198.51.100.0/24 — TEST-NET-2
        if (a == 198 && b == 51 && bytes[2] == 100) return false;
        // 203.0.113.0/24 — TEST-NET-3
        if (a == 203 && b == 0 && bytes[2] == 113) return false;
        // 100.64.0.0/10 — CGNAT
        if (a == 100 && (b & 0xC0) == 64) return false;
        // 224.0.0.0/4 — multicast / 240.0.0.0/4 — reserved / 255.255.255.255
        if (a >= 224) return false;

        return true;
    }

    private static bool IsGloballyRoutableIPv6(IPAddress address)
    {
        // IPv6 loopback handled by IPAddress.IsLoopback above.
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return false;
        }

        Span<byte> bytes = stackalloc byte[16];
        if (!address.TryWriteBytes(bytes, out _))
        {
            return false;
        }

        // Unique local addresses: fc00::/7
        if ((bytes[0] & 0xFE) == 0xFC) return false;
        // Unspecified ::
        bool allZero = true;
        for (int i = 0; i < bytes.Length; i++) { if (bytes[i] != 0) { allZero = false; break; } }
        if (allZero) return false;
        // Discard prefix 100::/64
        if (bytes[0] == 0x01 && bytes[1] == 0x00 && bytes[2] == 0 && bytes[3] == 0 && bytes[4] == 0 && bytes[5] == 0 && bytes[6] == 0 && bytes[7] == 0)
        {
            return false;
        }
        // Documentation 2001:db8::/32
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8) return false;

        return true;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Builds a <c>ConnectCallback</c> that rejects non-globally-routable DNS resolutions.
    /// </summary>
    /// <param name="allowLoopback">
    /// Whether loopback targets are permitted (set when the base-address host is loopback).
    /// </param>
    public static Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>> BuildConnectCallback(
        bool allowLoopback)
    {
        return async (context, cancellationToken) =>
        {
            DnsEndPoint endpoint = context.DnsEndPoint;
            IPAddress[] addresses = await Dns
                .GetHostAddressesAsync(endpoint.Host, cancellationToken)
                .ConfigureAwait(false);

            IPAddress? chosen = null;
            foreach (IPAddress addr in addresses)
            {
                if (IsGloballyRoutable(addr, allowLoopback))
                {
                    chosen = addr;
                    break;
                }
            }

            if (chosen is null)
            {
                throw new OllamaConfigurationException(
                    $"Connection to '{endpoint.Host}:{endpoint.Port}' rejected: host resolved " +
                    $"to {addresses.Length} address(es), none of which are globally routable. " +
                    "Disable OllamaClientOptions.DisallowPrivateNetworks or point BaseAddress at a public endpoint.");
            }

            Socket socket = new(chosen.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
            };

            try
            {
                await socket.ConnectAsync(new IPEndPoint(chosen, endpoint.Port), cancellationToken)
                    .ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        };
    }
#endif
}
