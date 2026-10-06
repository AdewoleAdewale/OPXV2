using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Opx.Services;

/// <summary>Result of an API call: HTTP status, parsed body (when it parses) and a user-facing error message.</summary>
public class OpxResult<T> where T : class
{
    public HttpStatusCode StatusCode { get; init; }
    public bool IsHttpSuccess { get; init; }
    public T? Data { get; init; }
    public string RawBody { get; init; } = "";
    /// <summary>Best message extracted from the body ("message" / "error" / validation errors) or a status fallback.</summary>
    public string ErrorMessage { get; init; } = "";
    public bool IsNetworkError { get; init; }
}

/// <summary>
/// One shared HttpClient for every OPX call.
/// It owns a CookieContainer so the auth cookie issued by /AuthAccount/Login is sent on later calls
/// (required by /ChangePassword, which is "the currently signed-in user").
/// </summary>
public static class OpxApi
{
    public const string BaseUrl = "https://opxng.com/api";

    private static readonly CookieContainer Cookies = new();

    private static readonly JsonSerializerSettings WriteSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    public static HttpClient Client { get; } = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = Cookies,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.Accept.Add(new("application/json"));
        return client;
    }

    /// <summary>Drop the session cookie (call on logout).</summary>
    public static void ClearSession()
    {
        foreach (Cookie c in Cookies.GetCookies(new Uri(BaseUrl)))
            c.Expired = true;
    }

    /// <summary>A cookie in a form that can be saved as JSON (used to keep the user signed in across launches).</summary>
    public class StoredCookie
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Path { get; set; } = "/";
        public DateTime Expires { get; set; }
        public bool Secure { get; set; }
        public bool HttpOnly { get; set; }
    }

    /// <summary>Snapshot of the live cookies for the OPX API (auth cookie included).</summary>
    public static List<StoredCookie> ExportCookies()
    {
        var list = new List<StoredCookie>();
        foreach (Cookie c in Cookies.GetCookies(new Uri(BaseUrl)))
        {
            if (c.Expired) continue;
            list.Add(new StoredCookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Expires = c.Expires,
                Secure = c.Secure,
                HttpOnly = c.HttpOnly
            });
        }
        return list;
    }

    /// <summary>Put previously saved cookies back so the server still recognises the signed-in user.</summary>
    public static void ImportCookies(IEnumerable<StoredCookie>? saved)
    {
        if (saved == null) return;
        foreach (var s in saved)
        {
            try
            {
                if (s.Expires != default && s.Expires < DateTime.Now) continue;   // already expired
                var c = new Cookie(s.Name, s.Value, string.IsNullOrEmpty(s.Path) ? "/" : s.Path,
                                   string.IsNullOrEmpty(s.Domain) ? new Uri(BaseUrl).Host : s.Domain)
                { Secure = s.Secure, HttpOnly = s.HttpOnly };
                if (s.Expires != default) c.Expires = s.Expires;
                Cookies.Add(c);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImportCookies skipped '{s.Name}': {ex.Message}");
            }
        }
    }

    public static Task<OpxResult<T>> GetAsync<T>(string path, CancellationToken ct = default) where T : class
        => SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, BaseUrl + path), ct);

    public static Task<OpxResult<T>> PostAsync<T>(string path, object? body, CancellationToken ct = default) where T : class
    {
        var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + path)
        {
            Content = new StringContent(
                body == null ? "{}" : JsonConvert.SerializeObject(body, WriteSettings),
                Encoding.UTF8, "application/json")
        };
        return SendAsync<T>(req, ct);
    }

    private static async Task<OpxResult<T>> SendAsync<T>(HttpRequestMessage req, CancellationToken ct) where T : class
    {
        try
        {
            using var resp = await Client.SendAsync(req, ct).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            T? data = null;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try { data = JsonConvert.DeserializeObject<T>(body); }
                catch (JsonException) { /* body isn't the expected shape (e.g. plain-text or ProblemDetails) */ }
            }

            return new OpxResult<T>
            {
                StatusCode = resp.StatusCode,
                IsHttpSuccess = resp.IsSuccessStatusCode,
                Data = data,
                RawBody = body,
                ErrorMessage = resp.IsSuccessStatusCode ? "" : ExtractError(body, resp.StatusCode)
            };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return NetworkFailure<T>("The request timed out. Please check your connection and try again.");
        }
        catch (HttpRequestException ex)
        {
            return NetworkFailure<T>($"Could not reach the server: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Anything else (socket/IO/TLS/etc.) must never crash the app – report it like a network failure.
            System.Diagnostics.Debug.WriteLine($"OpxApi unexpected error: {ex}");
            return NetworkFailure<T>("Something went wrong while contacting the server. Please try again.");
        }
    }

    private static OpxResult<T> NetworkFailure<T>(string msg) where T : class
        => new() { StatusCode = 0, IsHttpSuccess = false, IsNetworkError = true, ErrorMessage = msg };

    /// <summary>Pulls a readable message out of the API's error shapes: {message}, {error}, ProblemDetails/validation {errors}, Identity error arrays, or a bare string.</summary>
    public static string ExtractError(string body, HttpStatusCode status)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var token = JToken.Parse(body);
                switch (token)
                {
                    case JValue v when v.Type == JTokenType.String:
                        return v.ToString();
                    case JObject o:
                        var msg = (string?)o["message"] ?? (string?)o["error"];
                        if (!string.IsNullOrWhiteSpace(msg)) return msg!;
                        if (o["errors"] is JObject validation)
                            return string.Join("\n", validation.Properties()
                                .SelectMany(p => p.Value.Select(x => x.ToString())));
                        if (o["errors"] is JArray errs)
                            return string.Join("\n", errs.Select(e => (string?)e["description"] ?? e.ToString()));
                        if (o["title"] != null) return (string)o["title"]!;
                        break;
                    case JArray arr:   // ASP.NET Identity: [{code, description}]
                        return string.Join("\n", arr.Select(e => (string?)e["description"] ?? e.ToString()));
                }
            }
            catch (JsonException) { return body.Length > 300 ? body[..300] : body; }
        }
        return status switch
        {
            HttpStatusCode.Unauthorized => "You are not signed in. Please log in again.",
            HttpStatusCode.NotFound => "The requested item was not found.",
            HttpStatusCode.BadGateway => "The payment provider is currently unavailable. Please try again shortly.",
            _ => $"Server returned {(int)status} {status}."
        };
    }
}