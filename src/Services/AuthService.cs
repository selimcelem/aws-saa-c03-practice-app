using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace AwsSaaC03Practice.Services;

public class UserInfo
{
    [JsonPropertyName("sub")]   public string Sub  { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("name")]  public string Name  { get; set; } = "";
}

public record TokenData
{
    public string AccessToken  { get; set; } = "";
    public string IdToken      { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}

public class AuthService
{
    private readonly SettingsService _settings;
    private readonly HttpClient _http = new();
    private const string TokenStorageKey = "auth_tokens";

    // Android OAuth callback — set by MainActivity.OnNewIntent
    private static TaskCompletionSource<Uri?>? _androidCallbackTcs;
    public static void HandleAndroidCallback(Uri uri) =>
        _androidCallbackTcs?.TrySetResult(uri);

    public AuthService(SettingsService settings) => _settings = settings;

    // ── Auto-login ─────────────────────────────────────────────────────────

    public async Task<bool> TryAutoLoginAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(TokenStorageKey);
            if (string.IsNullOrEmpty(json)) return false;
            var tokens = JsonSerializer.Deserialize<TokenData>(json)!;
            if (tokens.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5)) return true;
            return await RefreshAsync(tokens.RefreshToken);
        }
        catch { return false; }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var json = await SecureStorage.Default.GetAsync(TokenStorageKey);
        if (string.IsNullOrEmpty(json)) return null;
        var tokens = JsonSerializer.Deserialize<TokenData>(json)!;
        if (tokens.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5)) return tokens.AccessToken;
        if (!await RefreshAsync(tokens.RefreshToken)) return null;
        json = await SecureStorage.Default.GetAsync(TokenStorageKey);
        return JsonSerializer.Deserialize<TokenData>(json!)!.AccessToken;
    }

    public async Task<string?> GetIdTokenAsync()
    {
        var json = await SecureStorage.Default.GetAsync(TokenStorageKey);
        if (string.IsNullOrEmpty(json)) return null;
        var tokens = JsonSerializer.Deserialize<TokenData>(json)!;
        if (tokens.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5)) return tokens.IdToken;
        if (!await RefreshAsync(tokens.RefreshToken)) return null;
        json = await SecureStorage.Default.GetAsync(TokenStorageKey);
        return JsonSerializer.Deserialize<TokenData>(json!)!.IdToken;
    }

    public async Task<UserInfo?> GetUserInfoAsync()
    {
        var token = await GetAccessTokenAsync();
        if (token is null) return null;
        var req = new HttpRequestMessage(HttpMethod.Get,
            $"https://{_settings.Settings.CognitoDomain}/oauth2/userInfo");
        req.Headers.Authorization = new("Bearer", token);
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return JsonSerializer.Deserialize<UserInfo>(await resp.Content.ReadAsStringAsync());
    }

    // ── Sign-in flow ────────────────────────────────────────────────────────

    public async Task<bool> SignInWithGoogleAsync()
    {
        var verifier  = GenerateCodeVerifier();
        var challenge = GenerateCodeChallenge(verifier);

#if ANDROID
        var redirectUri = _settings.Settings.OAuthCallbackAndroid;
        var authUrl = BuildAuthUrl(challenge, redirectUri);
        _androidCallbackTcs = new TaskCompletionSource<Uri?>();
        // Use Chrome Custom Tabs for a cleaner in-app browser experience
        // (minimal toolbar instead of full browser with prominent URL bar)
        var customTabsIntent = new AndroidX.Browser.CustomTabs.CustomTabsIntent.Builder()
            .SetShowTitle(true)
            .Build();
        customTabsIntent.LaunchUrl(Platform.CurrentActivity!,
            Android.Net.Uri.Parse(authUrl)!);
        Uri? callbackUri;
        try
        {
            callbackUri = await _androidCallbackTcs.Task.WaitAsync(TimeSpan.FromMinutes(5));
        }
        catch (TimeoutException) { return false; }
        if (callbackUri is null) return false;
        var code = HttpUtility.ParseQueryString(callbackUri.Query)["code"];
#else
        var redirectUri = _settings.Settings.OAuthCallbackWindows;
        var authUrl = BuildAuthUrl(challenge, redirectUri);
        await Launcher.Default.OpenAsync(authUrl);
        var code = await WaitForCodeWindowsAsync(CancellationToken.None);
#endif
        if (string.IsNullOrEmpty(code)) return false;
        return await ExchangeCodeAsync(code, verifier, redirectUri);
    }

    public async Task SignOutAsync()
    {
        SecureStorage.Default.Remove(TokenStorageKey);
        await Task.CompletedTask;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private string BuildAuthUrl(string codeChallenge, string redirectUri)
    {
        var s = _settings.Settings;
        return $"https://{s.CognitoDomain}/oauth2/authorize" +
               $"?response_type=code" +
               $"&client_id={s.CognitoClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&scope=email+openid+profile" +
               $"&identity_provider=Google" +
               $"&code_challenge={codeChallenge}" +
               $"&code_challenge_method=S256";
    }

    private async Task<string?> WaitForCodeWindowsAsync(CancellationToken ct)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:7890/");
        listener.Start();
        try
        {
            var ctxTask = listener.GetContextAsync();
            if (await Task.WhenAny(ctxTask, Task.Delay(TimeSpan.FromMinutes(5), ct)) != ctxTask)
                return null;

            var ctx = ctxTask.Result;
            var code = ctx.Request.QueryString["code"];

            // Respond to browser so the tab can show a completion message
            var html = Encoding.UTF8.GetBytes(
                "<html><head><title>Auth complete</title></head>" +
                "<body style='background:#0d1117;color:#c9d1d9;font-family:monospace;" +
                "display:flex;align-items:center;justify-content:center;height:100vh;margin:0'>" +
                "<p>Authentication complete. You may close this tab.</p>" +
                "<script>setTimeout(()=>window.close(),1500)</script></body></html>");

            ctx.Response.ContentType = "text/html";
            ctx.Response.ContentLength64 = html.Length;
            await ctx.Response.OutputStream.WriteAsync(html, ct);
            ctx.Response.Close();
            return code;
        }
        finally { listener.Stop(); }
    }

    private async Task<bool> ExchangeCodeAsync(string code, string verifier, string redirectUri)
    {
        var s = _settings.Settings;
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["client_id"]     = s.CognitoClientId,
            ["code"]          = code,
            ["redirect_uri"]  = redirectUri,
            ["code_verifier"] = verifier,
        });

        var resp = await _http.PostAsync($"https://{s.CognitoDomain}/oauth2/token", form);
        if (!resp.IsSuccessStatusCode) return false;

        var json  = await resp.Content.ReadAsStringAsync();
        var raw   = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        var tokens = new TokenData
        {
            AccessToken  = raw["access_token"].GetString()!,
            IdToken      = raw.ContainsKey("id_token") ? raw["id_token"].GetString()! : "",
            RefreshToken = raw["refresh_token"].GetString()!,
            ExpiresAt    = DateTimeOffset.UtcNow.AddSeconds(raw["expires_in"].GetInt32() - 30),
        };
        await SecureStorage.Default.SetAsync(TokenStorageKey, JsonSerializer.Serialize(tokens));
        return true;
    }

    private async Task<bool> RefreshAsync(string refreshToken)
    {
        try
        {
            var s    = _settings.Settings;
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "refresh_token",
                ["client_id"]     = s.CognitoClientId,
                ["refresh_token"] = refreshToken,
            });
            var resp = await _http.PostAsync($"https://{s.CognitoDomain}/oauth2/token", form);
            if (!resp.IsSuccessStatusCode) return false;

            var json = await resp.Content.ReadAsStringAsync();
            var raw  = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
            var existing = JsonSerializer.Deserialize<TokenData>(
                await SecureStorage.Default.GetAsync(TokenStorageKey) ?? "{}")!;

            var updated = existing with
            {
                AccessToken = raw["access_token"].GetString()!,
                IdToken     = raw.ContainsKey("id_token") ? raw["id_token"].GetString()! : existing.IdToken,
                ExpiresAt   = DateTimeOffset.UtcNow.AddSeconds(raw["expires_in"].GetInt32() - 30),
            };
            await SecureStorage.Default.SetAsync(TokenStorageKey, JsonSerializer.Serialize(updated));
            return true;
        }
        catch { return false; }
    }

    // ── PKCE helpers ────────────────────────────────────────────────────────

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
