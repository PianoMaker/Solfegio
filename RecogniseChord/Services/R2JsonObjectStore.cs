using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;

namespace RecogniseChord.Services;

public sealed class R2JsonObjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    public R2JsonObjectStore(IAmazonS3 client, IConfiguration configuration)
    {
        _client = client;
        _bucketName = configuration["CloudflareR2:BucketName"]
            ?? throw new InvalidOperationException("CloudflareR2:BucketName is not configured.");
    }

    public async Task PutAsync<T>(string key, T value)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            ContentBody = JsonSerializer.Serialize(value, JsonOptions),
            ContentType = "application/json"
        });
    }

    public async Task PutIfMissingAsync<T>(string key, T value)
    {
        if (await GetAsync<T>(key) is null)
            await PutAsync(key, value);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            using var response = await _client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            });
            return await JsonSerializer.DeserializeAsync<T>(response.ResponseStream, JsonOptions);
        }
        catch (AmazonS3Exception exception) when (
            exception.StatusCode == HttpStatusCode.NotFound ||
            exception.ErrorCode is "NoSuchKey" or "NotFound")
        {
            return default;
        }
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(string prefix)
    {
        var values = new List<T>();
        string? continuationToken = null;

        do
        {
            var response = await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix,
                ContinuationToken = continuationToken
            });

            foreach (var item in response.S3Objects)
            {
                var value = await GetAsync<T>(item.Key);
                if (value is not null)
                    values.Add(value);
            }

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return values;
    }
}
