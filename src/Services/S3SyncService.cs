using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AwsSaaC03Practice.Models;

namespace AwsSaaC03Practice.Services;

/// <summary>
/// Backs up session history to S3 as JSON under scores/{userSub}/sessions.json.
/// Uses the default AWS credential chain (~/.aws/credentials or environment vars).
/// </summary>
public class S3SyncService
{
    private readonly SettingsService _settings;
    private IAmazonS3? _s3;

    public S3SyncService(SettingsService settings) => _settings = settings;

    public string SyncStatus { get; private set; } = "Not synced";

    private IAmazonS3 GetClient()
    {
        if (_s3 is not null) return _s3;
        var region = RegionEndpoint.GetBySystemName(_settings.Settings.AwsRegion);
        _s3 = new AmazonS3Client(new EnvironmentVariablesAWSCredentials(), region);
        return _s3;
    }

    public async Task UploadSessionsAsync(string userSub, List<QuizSession> sessions)
    {
        try
        {
            SyncStatus = "Syncing…";
            var json = JsonSerializer.Serialize(sessions,
                new JsonSerializerOptions { WriteIndented = false });
            var key = $"scores/{userSub}/sessions.json";

            var req = new PutObjectRequest
            {
                BucketName  = _settings.Settings.S3BucketName,
                Key         = key,
                ContentBody = json,
                ContentType = "application/json",
            };
            await GetClient().PutObjectAsync(req);
            SyncStatus = $"Synced {DateTime.Now:HH:mm}";
        }
        catch (Exception ex)
        {
            SyncStatus = $"Sync failed: {ex.Message}";
        }
    }

    public async Task<List<QuizSession>?> DownloadSessionsAsync(string userSub)
    {
        try
        {
            var key = $"scores/{userSub}/sessions.json";
            var req = new GetObjectRequest
            {
                BucketName = _settings.Settings.S3BucketName,
                Key        = key,
            };
            using var resp = await GetClient().GetObjectAsync(req);
            using var reader = new StreamReader(resp.ResponseStream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<List<QuizSession>>(json);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null; // First login — no previous data
        }
        catch
        {
            return null;
        }
    }
}
