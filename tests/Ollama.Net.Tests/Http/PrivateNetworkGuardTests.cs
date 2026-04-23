using System.Net;
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
}
