namespace RecogniseChord.Models;

public sealed class CertifiedQuizChordAttempt
{
    public required int ChordCount { get; init; }
    public required bool Correct { get; init; }
    public required DateTimeOffset AttemptedAtUtc { get; init; }
}
