using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;

namespace AwsSaaC03Practice.Services;

public class ReportService
{
    private readonly SettingsService _settings;
    private readonly AuthService _auth;
    private IAmazonS3? _s3;

    public ReportService(SettingsService settings, AuthService auth)
    {
        _settings = settings;
        _auth = auth;
    }

    private async Task<IAmazonS3?> GetClientAsync()
    {
        if (_s3 is not null) return _s3;

        var idToken = await _auth.GetIdTokenAsync();
        if (string.IsNullOrEmpty(idToken)) return null;

        var s = _settings.Settings;
        var region = RegionEndpoint.GetBySystemName(s.AwsRegion);
        var providerName = $"cognito-idp.{s.AwsRegion}.amazonaws.com/{s.CognitoUserPoolId}";

        var credentials = new CognitoAWSCredentials(s.CognitoIdentityPoolId, region);
        credentials.AddLogin(providerName, idToken);

        _s3 = new AmazonS3Client(credentials, region);
        return _s3;
    }

    public async Task SubmitReportAsync(string questionId, string? comment, string userSub, string appVersion)
    {
        var client = await GetClientAsync();
        if (client is null) return;

        var bucket = _settings.Settings.S3BucketName;
        var key = $"reports/{questionId}.json";

        // Try to download existing reports array
        List<Dictionary<string, object>> reports;
        try
        {
            var resp = await client.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = key });
            using var reader = new StreamReader(resp.ResponseStream);
            var json = await reader.ReadToEndAsync();
            reports = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json) ?? new();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            reports = new();
        }

        // Append new report
        reports.Add(new Dictionary<string, object>
        {
            ["questionId"] = questionId,
            ["reportedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["userSub"] = userSub,
            ["comment"] = comment ?? "",
            ["appVersion"] = appVersion,
        });

        // Upload
        var uploadJson = JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = false });
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            ContentBody = uploadJson,
            ContentType = "application/json",
        });
    }

    public void ClearCache()
    {
        _s3?.Dispose();
        _s3 = null;
    }
}
