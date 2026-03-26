using System.Text.Json;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using AwsSaaC03Practice.Models;

namespace AwsSaaC03Practice.Services;

/// <summary>
/// Backs up session history to S3 under scores/{identityId}/sessions.json.
/// Uses Cognito Identity Pool to get temporary, per-user AWS credentials.
/// The IAM policy scopes each user to their own folder only.
/// </summary>
public class S3SyncService
{
    private readonly SettingsService _settings;
    private readonly AuthService _auth;
    private IAmazonS3? _s3;
    private string? _identityId;

    public S3SyncService(SettingsService settings, AuthService auth)
    {
        _settings = settings;
        _auth = auth;
    }

    public string SyncStatus { get; private set; } = "Not synced";

    private async Task<(IAmazonS3 client, string identityId)?> GetAuthenticatedClientAsync()
    {
        if (_s3 is not null && _identityId is not null)
            return (_s3, _identityId);

        var idToken = await _auth.GetIdTokenAsync();
        if (string.IsNullOrEmpty(idToken)) return null;

        var s = _settings.Settings;
        if (string.IsNullOrEmpty(s.CognitoIdentityPoolId))
        {
            SyncStatus = "Sync skipped (Identity Pool not configured)";
            return null;
        }
        var region = RegionEndpoint.GetBySystemName(s.AwsRegion);
        var providerName = $"cognito-idp.{s.AwsRegion}.amazonaws.com/{s.CognitoUserPoolId}";

        var credentials = new CognitoAWSCredentials(s.CognitoIdentityPoolId, region);
        credentials.AddLogin(providerName, idToken);

        // Resolve the identity ID (used as S3 key prefix)
        _identityId = await credentials.GetIdentityIdAsync();
        _s3 = new AmazonS3Client(credentials, region);
        return (_s3, _identityId);
    }

    public async Task UploadSessionsAsync(string userSub, List<QuizSession> sessions)
    {
        try
        {
            SyncStatus = "Syncing\u2026";
            var result = await GetAuthenticatedClientAsync();
            if (result is null) { SyncStatus = "Sync skipped (not authenticated)"; return; }

            var (client, identityId) = result.Value;
            var json = JsonSerializer.Serialize(sessions,
                new JsonSerializerOptions { WriteIndented = false });
            var key = $"scores/{identityId}/sessions.json";

            var req = new PutObjectRequest
            {
                BucketName  = _settings.Settings.S3BucketName,
                Key         = key,
                ContentBody = json,
                ContentType = "application/json",
            };
            await client.PutObjectAsync(req);
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
            var result = await GetAuthenticatedClientAsync();
            if (result is null) return null;

            var (client, identityId) = result.Value;
            var key = $"scores/{identityId}/sessions.json";
            var req = new GetObjectRequest
            {
                BucketName = _settings.Settings.S3BucketName,
                Key        = key,
            };
            using var resp = await client.GetObjectAsync(req);
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

    public async Task DeleteUserDataAsync()
    {
        try
        {
            var result = await GetAuthenticatedClientAsync();
            if (result is null) return;

            var (client, identityId) = result.Value;
            var key = $"scores/{identityId}/sessions.json";
            await client.DeleteObjectAsync(_settings.Settings.S3BucketName, key);
        }
        catch { /* best-effort: local data is already cleared */ }
    }

    /// <summary>Clears cached credentials (call on sign-out).</summary>
    public void ClearCache()
    {
        _s3?.Dispose();
        _s3 = null;
        _identityId = null;
    }
}
