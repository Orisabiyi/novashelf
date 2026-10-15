using System.Net;
using System.Net.Sockets;

namespace Novashelf.Api.Infrastructure;

// Every connection, including after redirects, is checked at connect time, so the server never
// reaches non-public addresses and DNS rebinding can't get around the check.
public static class OutboundGuard
{
    public static SocketsHttpHandler CreateHandler() => new()
    {
        ConnectCallback = ConnectToPublicAddressAsync,
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        UseCookies = false,
    };

    private static async ValueTask<Stream> ConnectToPublicAddressAsync(
        SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = await Dns.GetHostAddressesAsync(host, ct);

        // Refuse the host outright if any of its addresses is non-public, rather than
        // picking a "good" one and trusting the rest.
        if (addresses.Length == 0 || !addresses.All(IsPublicAddress))
            throw new HttpRequestException($"Refusing to connect to non-public address for '{host}'.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address)
            || address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.Broadcast))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(
                b[0] == 0                                   // 0.0.0.0/8 "this network"
                || b[0] == 10                               // 10.0.0.0/8 private
                || (b[0] == 100 && (b[1] & 0xC0) == 64)     // 100.64.0.0/10 carrier-grade NAT
                || b[0] == 127                              // 127.0.0.0/8 loopback
                || (b[0] == 169 && b[1] == 254)             // 169.254.0.0/16 link-local, cloud metadata
                || (b[0] == 172 && (b[1] & 0xF0) == 16)     // 172.16.0.0/12 private
                || (b[0] == 192 && b[1] == 0 && b[2] == 0)  // 192.0.0.0/24 IETF protocol assignments
                || (b[0] == 192 && b[1] == 168)             // 192.168.0.0/16 private
                || (b[0] == 198 && (b[1] & 0xFE) == 18)     // 198.18.0.0/15 benchmarking
                || b[0] >= 224);                            // multicast and reserved
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var b = address.GetAddressBytes();
            return !(
                address.IsIPv6LinkLocal                     // fe80::/10
                || address.IsIPv6SiteLocal                  // fec0::/10 (deprecated)
                || address.IsIPv6Multicast                  // ff00::/8
                || (b[0] & 0xFE) == 0xFC);                  // fc00::/7 unique local
        }

        return false;
    }
}
