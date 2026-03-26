using System.Text.Json;

namespace AwsSaaC03Practice.Services;

public class AppSettings
{
    public string AwsRegion { get; set; } = "eu-west-1";
    public string CognitoUserPoolId { get; set; } = "";
    public string CognitoClientId { get; set; } = "";
    public string CognitoDomain { get; set; } = "";
    public string S3BucketName { get; set; } = "";
    public string OAuthCallbackWindows { get; set; } = "http://localhost:7890";
    public string OAuthCallbackAndroid { get; set; } = "myapp://callback";
}

public class SettingsService
{
    private AppSettings? _settings;
    public AppSettings Settings => _settings
        ?? throw new InvalidOperationException("SettingsService not loaded. Call LoadAsync() first.");

    public async Task LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
        _settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new Exception("Failed to parse appsettings.json");

        if (string.IsNullOrEmpty(_settings.CognitoClientId))
            throw new Exception("appsettings.json is missing CognitoClientId. Copy appsettings.example.json, fill in values, and save as appsettings.json.");
    }
}
