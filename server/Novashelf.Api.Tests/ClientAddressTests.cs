using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Novashelf.Api.Accounts;

namespace Novashelf.Api.Tests;

// Header shapes as observed on Render (Cloudflare, then Render's load balancer) with and without
// Vercel's /api rewrite in front.
public class ClientAddressTests
{
    private const string Reader = "102.88.1.2";
    private const string VercelServer = "16.28.17.192";
    private const string Cloudflare = "104.23.166.105";
    private const string RenderBalancer = "10.24.36.188";

    private static ClientAddress Behind(string? edgeHeader) => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Proxy:ClientAddressHeader"] = edgeHeader })
        .Build());

    private static HttpContext Request(string? forwardedFor, string? connecting, string remote = "::1")
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        if (forwardedFor is not null) context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        if (connecting is not null) context.Request.Headers["CF-Connecting-IP"] = connecting;
        return context;
    }

    [Fact]
    public void Through_Vercel_the_reader_is_the_entry_before_Vercels_server()
    {
        var context = Request($"{Reader},{VercelServer}, {Cloudflare}, {RenderBalancer}", VercelServer);
        var addresses = Behind("CF-Connecting-IP");

        Assert.Equal(VercelServer, addresses.Connecting(context));
        Assert.Equal(Reader, addresses.Visitor(context));
    }

    [Fact]
    public void Direct_requests_are_the_connecting_address()
    {
        var context = Request($"{Reader}, {Cloudflare}, {RenderBalancer}", Reader);
        var addresses = Behind("CF-Connecting-IP");

        Assert.Equal(Reader, addresses.Connecting(context));
        Assert.Equal(Reader, addresses.Visitor(context));
    }

    [Fact]
    public void A_forged_forwarded_entry_cannot_change_the_connecting_address()
    {
        // Sent straight to the API with a made-up X-Forwarded-For: the visitor is forgeable (the
        // connecting-address limit covers that), but the connecting address is Cloudflare's view.
        var context = Request($"203.0.113.7,{Reader}, {Cloudflare}, {RenderBalancer}", Reader);
        var addresses = Behind("CF-Connecting-IP");

        Assert.Equal(Reader, addresses.Connecting(context));
        Assert.Equal("203.0.113.7", addresses.Visitor(context));
    }

    [Fact]
    public void Without_an_edge_header_configured_forwarding_headers_are_ignored()
    {
        var context = Request($"203.0.113.7, {Cloudflare}", "203.0.113.8", remote: "198.51.100.4");
        var addresses = Behind(null);

        Assert.Equal("198.51.100.4", addresses.Connecting(context));
        Assert.Equal("198.51.100.4", addresses.Visitor(context));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-address")]
    public void A_missing_or_invalid_edge_header_falls_back_to_the_connection(string? connecting)
    {
        var context = Request($"{Reader}, {Cloudflare}", connecting, remote: "198.51.100.4");

        Assert.Equal("198.51.100.4", Behind("CF-Connecting-IP").Connecting(context));
    }

    [Theory]
    [InlineData("garbage, 16.28.17.192")]
    [InlineData("16.28.17.192")]
    [InlineData("")]
    public void The_visitor_falls_back_to_the_connecting_address(string forwardedFor)
    {
        var context = Request(forwardedFor, VercelServer);

        Assert.Equal(VercelServer, Behind("CF-Connecting-IP").Visitor(context));
    }
}
