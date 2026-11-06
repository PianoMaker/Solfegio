using System.Text.Json;
using RecogniseChord.Models;

namespace RecogniseChord.Services;

public sealed class FailedQuizAttemptJsonStore : IFailedQuizAttemptStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly List<FailedQuizAttempt> _attempts;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public FailedQuizAttemptJsonStore(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["Certificates:FailedAttemptsStoragePath"];
        _filePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "failed-quiz-attempts.json")
            : Path.GetFullPath(Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath));

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        _attempts = File.Exists(_filePath)
            ? JsonSerializer.Deserialize<List<FailedQuizAttempt>>(File.ReadAllText(_filePath), JsonOptions) ?? []
            : [];
    }

    public async Task RecordAsync(FailedQuizAttempt attempt)
    {
        await _gate.WaitAsync();
        try
        {
            _attempts.Add(attempt);
            var temporaryPath = _filePath + ".tmp";
            try
            {
                await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    await JsonSerializer.SerializeAsync(stream, _attempts, JsonOptions);
                File.Move(temporaryPath, _filePath, overwrite: true);
            }
            catch
            {
                _attempts.RemoveAt(_attempts.Count - 1);
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<FailedQuizAttempt>> ListAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return _attempts.OrderByDescending(attempt => attempt.CompletedAtUtc).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }
}
