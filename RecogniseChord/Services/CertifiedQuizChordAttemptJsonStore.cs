using System.Text.Json;
using RecogniseChord.Models;

namespace RecogniseChord.Services;

public sealed class CertifiedQuizChordAttemptJsonStore : ICertifiedQuizChordAttemptStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly List<CertifiedQuizChordAttempt> _attempts;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public CertifiedQuizChordAttemptJsonStore(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["Certificates:QuizChordAttemptsStoragePath"];
        _filePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "certified-quiz-chord-attempts.json")
            : Path.GetFullPath(Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath));

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        _attempts = File.Exists(_filePath)
            ? JsonSerializer.Deserialize<List<CertifiedQuizChordAttempt>>(File.ReadAllText(_filePath), JsonOptions) ?? []
            : [];
    }

    public async Task RecordAsync(CertifiedQuizChordAttempt attempt)
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

    public async Task<IReadOnlyList<CertifiedQuizChordAttempt>> ListAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return _attempts.OrderByDescending(attempt => attempt.AttemptedAtUtc).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }
}
