using System.Net;

namespace Novashelf.Api.Accounts;

/// <summary>
/// Works out who is calling, for the per-address limits on sign-in and other account actions.
/// </summary>
/// <remarks>
/// <para>
/// In production, Cloudflare (in front of Render) puts the address that connected to it in
/// <c>CF-Connecting-IP</c> and rejects requests that try to set that header themselves. Name the
/// header in <c>Proxy:ClientAddressHeader</c>; leave it unset on hosts without such an edge, or
/// anyone could choose their own address.
/// </para>
/// <para>
/// For requests through the site's <c>/api</c> rewrite, the connecting address is one of Vercel's
/// servers, which change from request to request. Vercel replaces <c>X-Forwarded-For</c> with
/// the reader's address, so the entry just before the connecting address is the reader. A caller
/// going straight to the API can put anything there, which is why the connecting address gets
/// its own, looser limit as well.
/// </para>
/// </remarks>
public sealed class ClientAddress(IConfiguration config)
{
    private readonly string? _edgeHeader =
        config["Proxy:ClientAddressHeader"] is { Length: > 0 } header ? header : null;

    /// <summary>The address that connected to the edge (or to the API when there's no edge).</summary>
    public string Connecting(HttpContext context)
    {
        if (_edgeHeader is not null && Normalize(context.Request.Headers[_edgeHeader].ToString()) is { } address)
            return address;
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>The reader behind the connecting address, as reported by the proxy in front of it.</summary>
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
