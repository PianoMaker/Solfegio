using RecogniseChord.Models;

namespace RecogniseChord.Services;

public sealed class R2CertifiedQuizChordAttemptStore : ICertifiedQuizChordAttemptStore
{
    private readonly R2JsonObjectStore _objects;

    public R2CertifiedQuizChordAttemptStore(R2JsonObjectStore objects)
    {
        _objects = objects;
    }

    public Task RecordAsync(CertifiedQuizChordAttempt attempt) =>
        _objects.PutAsync($"certified-quiz-attempts/{Guid.NewGuid():N}.json", attempt);

    public async Task<IReadOnlyList<CertifiedQuizChordAttempt>> ListAsync()
    {
        var attempts = await _objects.ListAsync<CertifiedQuizChordAttempt>("certified-quiz-attempts/");
        return attempts.OrderByDescending(attempt => attempt.AttemptedAtUtc).ToArray();
    }
}
