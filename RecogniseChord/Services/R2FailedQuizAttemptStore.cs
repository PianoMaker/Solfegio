using System.Text.Json;
using RecogniseChord.Models;

namespace RecogniseChord.Services;

public sealed class R2FailedQuizAttemptStore : IFailedQuizAttemptStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly R2JsonObjectStore _objects;
    private readonly string _legacyFilePath;
    private readonly SemaphoreSlim _migrationGate = new(1, 1);
    private bool _legacyDataMigrated;

    public R2FailedQuizAttemptStore(
        R2JsonObjectStore objects,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _objects = objects;
        var configuredPath = configuration["Certificates:FailedAttemptsStoragePath"];
        _legacyFilePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "failed-quiz-attempts.json")
            : Path.GetFullPath(Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath));
    }

    public async Task RecordAsync(FailedQuizAttempt attempt)
    {
        await EnsureLegacyDataMigratedAsync();
        await _objects.PutAsync($"failed-attempts/{Guid.NewGuid():N}.json", attempt);
    }

    public async Task<IReadOnlyList<FailedQuizAttempt>> ListAsync()
    {
        await EnsureLegacyDataMigratedAsync();
        var attempts = await _objects.ListAsync<FailedQuizAttempt>("failed-attempts/");
        return attempts.OrderByDescending(attempt => attempt.CompletedAtUtc).ToArray();
    }

    private async Task EnsureLegacyDataMigratedAsync()
    {
        if (_legacyDataMigrated)
            return;

        await _migrationGate.WaitAsync();
        try
        {
            if (_legacyDataMigrated)
                return;

            if (File.Exists(_legacyFilePath))
            {
                var json = await File.ReadAllTextAsync(_legacyFilePath);
                var attempts = JsonSerializer.Deserialize<List<FailedQuizAttempt>>(json, JsonOptions) ?? [];
                for (var index = 0; index < attempts.Count; index++)
                {
                    var key = $"failed-attempts/legacy/{index:D8}.json";
                    await _objects.PutIfMissingAsync(key, attempts[index]);
                }
            }

            _legacyDataMigrated = true;
        }
        finally
        {
            _migrationGate.Release();
        }
    }
}
