using RecogniseChord.Models;

namespace RecogniseChord.Services;

public interface IFailedQuizAttemptStore
{
    Task RecordAsync(FailedQuizAttempt attempt);
    Task<IReadOnlyList<FailedQuizAttempt>> ListAsync();
}
