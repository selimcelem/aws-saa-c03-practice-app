using System.Text.Json;

namespace AwsSaaC03Practice.Services;

public class AppSettings
{
    public string AwsRegion { get; set; } = "eu-west-1";
    public string CognitoUserPoolId { get; set; } = "";
    public string CognitoClientId { get; set; } = "";
    public string CognitoIdentityPoolId { get; set; } = "";
    public string CognitoDomain { get; set; } = "";
    public string S3BucketName { get; set; } = "";
    public string OAuthCallbackWindows { get; set; } = "http://localhost:7890";
    public string OAuthCallbackAndroid { get; set; } = "selimcelemsaaapp://callback";
}

public class SettingsService
{
    private AppSettings? _settings;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AppSettings Settings => _settings
        ?? throw new InvalidOperationException("SettingsService not loaded. Call EnsureLoadedAsync() first.");

    public async Task EnsureLoadedAsync()
    {
        if (_settings is not null) return;
        await _lock.WaitAsync();
        try
        {
            if (_settings is not null) return;
            using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
            _settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("Failed to parse appsettings.json");

            if (string.IsNullOrEmpty(_settings.CognitoClientId))
                throw new Exception("appsettings.json is missing CognitoClientId.");
        }
        finally { _lock.Release(); }
    }

    // Keep for backward compat — delegates to EnsureLoadedAsync
    public Task LoadAsync() => EnsureLoadedAsync();
}
