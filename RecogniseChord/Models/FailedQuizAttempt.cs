namespace RecogniseChord.Models;

public sealed class FailedQuizAttempt
{
    public required string ParticipantName { get; init; }
    public string? Email { get; init; }
    public required string Level { get; init; }
    public required int CorrectCount { get; init; }
    public required int Total { get; init; }
    public required DateTimeOffset CompletedAtUtc { get; init; }
}
