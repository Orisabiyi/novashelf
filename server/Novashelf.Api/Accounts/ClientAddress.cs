using System.Net;

namespace Novashelf.Api.Accounts;

// Proxy:ClientAddressHeader names a header the edge sets and won't let callers forge (Cloudflare's
// CF-Connecting-IP); leave it unset on hosts without such an edge. Through Vercel's /api rewrite
// the connecting address is a Vercel server, and the entry before it in X-Forwarded-For is the
// reader. Direct callers can forge that entry, so the connecting address has its own limit too.
public sealed class ClientAddress(IConfiguration config)
{
    private readonly string? _edgeHeader =
        config["Proxy:ClientAddressHeader"] is { Length: > 0 } header ? header : null;

    public string Connecting(HttpContext context)
    {
        if (_edgeHeader is not null && Normalize(context.Request.Headers[_edgeHeader].ToString()) is { } address)
            return address;
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public string Visitor(HttpContext context)
    {
        var connecting = Connecting(context);
        if (_edgeHeader is null) return connecting;

        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString()
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .ToList();
        var index = forwarded.LastIndexOf(connecting);
        return index > 0 && forwarded[index - 1] is { } visitor ? visitor : connecting;
    }

    private static string? Normalize(string value) =>
        IPAddress.TryParse(value.Trim(), out var address) ? address.ToString() : null;
}
