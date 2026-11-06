using RecogniseChord.Models;

namespace RecogniseChord.Services;

public interface ICertifiedQuizChordAttemptStore
{
    Task RecordAsync(CertifiedQuizChordAttempt attempt);
    Task<IReadOnlyList<CertifiedQuizChordAttempt>> ListAsync();
}
