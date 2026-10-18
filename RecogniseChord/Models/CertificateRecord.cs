namespace RecogniseChord.Models;

public sealed class CertificateRecord
{
    public required string Number { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public required string RecipientName { get; init; }
    public required string Level { get; init; }
    public required int CorrectCount { get; init; }
    public required int Total { get; init; }
    public required DateTimeOffset IssuedAtUtc { get; init; }
}
