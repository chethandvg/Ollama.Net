using System.Linq;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Ollama.Net.Http;
using Xunit;

namespace Ollama.Net.Tests.Http;

/// <summary>Unit tests for <see cref="PrivateNetworkGuard"/> IP classifier.</summary>
public sealed class PrivateNetworkGuardTests
{
    [Theory]
    // RFC1918 / link-local / CGNAT / test-net / multicast / reserved — all rejected
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.0.1")]
    [InlineData("192.168.255.255")]
    [InlineData("169.254.0.1")]
    [InlineData("100.64.0.1")]
    [InlineData("100.127.255.255")]
    [InlineData("0.0.0.0")]
    [InlineData("192.0.2.1")]      // TEST-NET-1
    [InlineData("198.18.0.1")]     // benchmark
    [InlineData("198.51.100.1")]   // TEST-NET-2
    [InlineData("203.0.113.1")]    // TEST-NET-3
    [InlineData("224.0.0.1")]      // multicast
    [InlineData("240.0.0.1")]      // reserved
    [InlineData("255.255.255.255")]
    [InlineData("::")]
    [InlineData("fc00::1")]        // unique-local
    [InlineData("fe80::1")]        // link-local
    [InlineData("ff02::1")]        // multicast
    [InlineData("2001:db8::1")]    // documentation
    public void IsGloballyRoutable_Rejects_NonRoutable(string addr)
    {
        IPAddress ip = IPAddress.Parse(addr);
        PrivateNetworkGuard.IsGloballyRoutable(ip, allowLoopback: false).Should().BeFalse();
        PrivateNetworkGuard.IsGloballyRoutable(ip, allowLoopback: true).Should().BeFalse();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("52.96.0.1")]
    [InlineData("172.15.255.255")] // just outside 172.16/12
    [InlineData("172.32.0.1")]     // just outside 172.16/12
    [InlineData("192.167.255.255")]
    [InlineData("192.169.0.1")]
    [InlineData("100.63.255.255")] // just outside 100.64/10
    [InlineData("100.128.0.1")]    // just outside 100.64/10
    [InlineData("2606:4700:4700::1111")]
    public void IsGloballyRoutable_Accepts_Public(string addr)
    {
        IPAddress ip = IPAddress.Parse(addr);
        PrivateNetworkGuard.IsGloballyRoutable(ip, allowLoopback: false).Should().BeTrue();
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.1.2.3")]
    [InlineData("::1")]
    public void IsGloballyRoutable_Loopback_RespectsFlag(string addr)
    {
        IPAddress ip = IPAddress.Parse(addr);
        PrivateNetworkGuard.IsGloballyRoutable(ip, allowLoopback: false).Should().BeFalse();
        PrivateNetworkGuard.IsGloballyRoutable(ip, allowLoopback: true).Should().BeTrue();
    }

    [Fact]
    public void IsGloballyRoutable_IPv4MappedIPv6_UsesIPv4Rules()
    {
        IPAddress mappedPublic = IPAddress.Parse("::ffff:8.8.8.8");
        IPAddress mappedPrivate = IPAddress.Parse("::ffff:10.0.0.1");

        PrivateNetworkGuard.IsGloballyRoutable(mappedPublic, allowLoopback: false).Should().BeTrue();
        PrivateNetworkGuard.IsGloballyRoutable(mappedPrivate, allowLoopback: false).Should().BeFalse();
    }

    [Fact]
    public async Task ConnectToFirstAllowed_ThrowsConfigurationException_WhenAllAddressesRejected()
    {
        IPAddress[] allPrivate =
        {
            IPAddress.Parse("10.0.0.1"),
            IPAddress.Parse("192.168.1.1"),
        };

        Func<Task> act = async () => await PrivateNetworkGuard.ConnectToFirstAllowedAsync(
            "blocked.example", 80, allPrivate, allowLoopback: false, CancellationToken.None).ConfigureAwait(false);

        (await act.Should().ThrowAsync<Ollama.Net.Exceptions.OllamaConfigurationException>())
            .Which.Message.Should().Contain("none of which are globally routable");
    }

    [Fact]
    public async Task ConnectToFirstAllowed_TriesEveryAllowedCandidate_WhenAllFail()
    {
        // Pick one loopback port that is guaranteed closed.
        int closedPort = GetFreePort();

        // Supply three allowed candidates (all loopback). Every connect must
        // fail fast with ECONNREFUSED; the aggregate must carry one inner
        // exception per candidate, proving the fallback loop iterates every
        // address instead of bailing on the first failure.
        // Three identical loopback entries — we're not testing distinct
        // addresses, we're asserting the fallback loop keeps iterating after
        // each failure. Aggregate must have exactly `addresses.Length` inner
        // exceptions once every candidate has been tried.
        IPAddress[] addresses = Enumerable.Repeat(IPAddress.Loopback, 3).ToArray();

        Func<Task> act = async () => await PrivateNetworkGuard.ConnectToFirstAllowedAsync(
            "loopback.test", closedPort, addresses, allowLoopback: true, CancellationToken.None).ConfigureAwait(false);

        AggregateException agg = (await act.Should().ThrowAsync<AggregateException>()).Which;
        agg.InnerExceptions.Should().HaveCount(addresses.Length);
        agg.InnerExceptions.Should().AllSatisfy(e => e.Should().BeAssignableTo<SocketException>());
    }

    [Fact]
    public async Task ConnectToFirstAllowed_Succeeds_AfterFilteringRejectedAddresses()
    {
        // First address is private → filtered out by the allow-list; second is
        // loopback with a live listener. Proves the guard drops rejected
        // addresses and still connects via the surviving candidate.
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        try
        {
            IPAddress[] addresses = { IPAddress.Parse("10.0.0.1"), IPAddress.Loopback };
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();

            await using Stream stream = await PrivateNetworkGuard.ConnectToFirstAllowedAsync(
                "loopback.test", port, addresses, allowLoopback: true, CancellationToken.None).ConfigureAwait(false);

            stream.Should().BeAssignableTo<NetworkStream>();
            using TcpClient accepted = await acceptTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            accepted.Connected.Should().BeTrue();
        }
        finally
        {
            listener.Stop();
        }
    }

    private static int GetFreePort()
    {
        // Bind a probe listener to :0 (OS-assigned), record the port, release
        // it. There is a theoretical race where another process grabs the port
        // before the test reconnects; in practice (single-threaded test, sub-ms
        // window, full ephemeral range available) this is vanishingly rare and
        // would only manifest as an unrelated test failure. Documented rather
        // than defended against because any retry-on-race scaffolding would be
        // noise compared with the test's actual intent.
        using TcpListener probe = new(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
