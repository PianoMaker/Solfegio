using RecogniseChord.Models;

namespace RecogniseChord.Services;

public interface ICertificateStore
{
    Task<CertificateRecord> GetOrCreateForOwnerAndLevelAsync(string ownerId, string recipientName, string level, int correctCount, int total);
    Task<CertificateRecord?> FindAsync(string number);
}
