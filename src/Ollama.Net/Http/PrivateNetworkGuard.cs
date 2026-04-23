using System.Diagnostics.CodeAnalysis;
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
        // 172.16.0.0/12 — RFC1918. The /12 prefix means the upper nibble of the
        // second octet is fixed at 0001, i.e. b ∈ [16, 31] (0x10..0x1F), which is
        // equivalent to `(b & 0xF0) == 0x10` (== 16).
        if (a == 172 && (b & 0xF0) == 16) return false;
        // 192.0.0.0/24 — IETF protocol assignments
        if (a == 192 && b == 0 && bytes[2] == 0) return false;
        // 192.0.2.0/24 — TEST-NET-1
        if (a == 192 && b == 0 && bytes[2] == 2) return false;
        // 192.168.0.0/16 — RFC1918
        if (a == 192 && b == 168) return false;
        // 198.18.0.0/15 — benchmark (b ∈ {18, 19})
        if (a == 198 && (b & 0xFE) == 18) return false;
        // 198.51.100.0/24 — TEST-NET-2
        if (a == 198 && b == 51 && bytes[2] == 100) return false;
        // 203.0.113.0/24 — TEST-NET-3
        if (a == 203 && b == 0 && bytes[2] == 113) return false;
        // 100.64.0.0/10 — CGNAT. /10 fixes the top two bits of the second octet at
        // 01, i.e. b ∈ [64, 127] (0x40..0x7F), equivalent to `(b & 0xC0) == 0x40`.
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
        if (bytes.IndexOfAnyExcept((byte)0) < 0) return false;
        // Discard prefix 100::/64 — first 8 bytes = 01 00 00 00 00 00 00 00
        Span<byte> discardPrefix = stackalloc byte[8] { 0x01, 0, 0, 0, 0, 0, 0, 0 };
        if (bytes[..8].SequenceEqual(discardPrefix)) return false;
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

            return await ConnectToFirstAllowedAsync(
                endpoint.Host, endpoint.Port, addresses, allowLoopback, cancellationToken)
                .ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Filters <paramref name="addresses"/> through the allow-list then attempts to
    /// connect to each surviving candidate in DNS order, returning a
    /// <see cref="NetworkStream"/> for the first success. Exposed as
    /// <c>internal</c> so unit tests can exercise the address-fallback loop without
    /// mocking DNS.
    /// </summary>
    /// <exception cref="OllamaConfigurationException">
    /// Thrown when no DNS result passes the allow-list.
    /// </exception>
    /// <exception cref="AggregateException">
    /// Thrown when every allowed candidate fails to connect; the inner exceptions
    /// are the underlying <see cref="SocketException"/>s / <see cref="IOException"/>s.
    /// </exception>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Socket ownership is transferred to the returned NetworkStream on the success path; on every failure path we call socket.Dispose() explicitly.")]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code",
        Justification = "'failures' is lazily allocated inside the 'when' catch filter, which the analyzer does not trace.")]
    internal static async ValueTask<Stream> ConnectToFirstAllowedAsync(
        string host,
        int port,
        IReadOnlyList<IPAddress> addresses,
        bool allowLoopback,
        CancellationToken cancellationToken)
    {
        // Collect every DNS result that passes the allow-list. We keep the
        // original DNS order (rather than preferring a specific family) so
        // operators keep full control via resolver/search-order policy.
        List<IPAddress> candidates = new(addresses.Count);
        for (int i = 0; i < addresses.Count; i++)
        {
            if (IsGloballyRoutable(addresses[i], allowLoopback))
            {
                candidates.Add(addresses[i]);
            }
        }

        if (candidates.Count == 0)
        {
            throw new OllamaConfigurationException(
                $"Connection to '{host}:{port}' rejected: host resolved " +
                $"to {addresses.Count} address(es), none of which are globally routable. " +
                "Disable OllamaClientOptions.DisallowPrivateNetworks or point BaseAddress at a public endpoint.");
        }

        // Try candidates in DNS order. A single broken family (e.g. AAAA on
        // an IPv4-only host) should not make DisallowPrivateNetworks regress
        // dual-stack reliability compared with the default SocketsHttpHandler,
        // which itself fans out over all addresses. We stop at the first
        // successful connect and aggregate all errors if every attempt fails.
        List<Exception>? failures = null;
        for (int i = 0; i < candidates.Count; i++)
        {
            IPAddress candidate = candidates[i];
            Socket socket = new(candidate.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
            };

            try
            {
                await socket.ConnectAsync(new IPEndPoint(candidate, port), cancellationToken)
                    .ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (OperationCanceledException)
            {
                socket.Dispose();
                throw;
            }
            catch (Exception ex) when (ex is SocketException or IOException)
            {
                socket.Dispose();
                (failures ??= new List<Exception>()).Add(ex);
                // Fall through to the next candidate.
            }
        }

        throw new AggregateException(
            $"Failed to connect to '{host}:{port}' after trying {candidates.Count} allowed address(es).",
            failures!);
    }
#endif
}
