namespace RaceDay.Services;

public class ApiCookieHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    public const string SessionKey = "ApiCookie";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = accessor.HttpContext?.Session;
        var stored = session?.GetString(SessionKey);

        if (!string.IsNullOrEmpty(stored))
            request.Headers.TryAddWithoutValidation("Cookie", stored);

        var response = await base.SendAsync(request, cancellationToken);

        if (session is not null && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            session.SetString(SessionKey, Merge(stored, setCookies));

        return response;
    }

    // Combine the cookie we already hold with whatever the API just sent.
    private static string Merge(string? existing, IEnumerable<string> setCookies)
    {
        var jar = new Dictionary<string, string>();

        foreach (var pair in (existing ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var i = pair.IndexOf('=');
            if (i > 0) jar[pair[..i]] = pair[(i + 1)..];
        }

        foreach (var setCookie in setCookies)
        {
            var first = setCookie.Split(';')[0].Trim();   // "name=value" (drop path, expires, etc.)
            var i = first.IndexOf('=');
            if (i <= 0) continue;

            var name = first[..i];
            var value = first[(i + 1)..];
            if (value.Length == 0) jar.Remove(name); else jar[name] = value;
        }

        return string.Join("; ", jar.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}