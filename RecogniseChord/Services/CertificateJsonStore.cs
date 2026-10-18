using System.Security.Cryptography;
using System.Text.Json;
using RecogniseChord.Models;

namespace RecogniseChord.Services;

public sealed class CertificateJsonStore : ICertificateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly Dictionary<string, CertificateRecord> _certificates;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public CertificateJsonStore(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["Certificates:StoragePath"];
        _filePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "certificates.json")
            : Path.GetFullPath(Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath));

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        _certificates = File.Exists(_filePath)
            ? (JsonSerializer.Deserialize<List<CertificateRecord>>(File.ReadAllText(_filePath), JsonOptions) ?? [])
                .Where(certificate => !string.IsNullOrWhiteSpace(certificate.Number))
                .ToDictionary(certificate => certificate.Number, StringComparer.Ordinal)
            : new Dictionary<string, CertificateRecord>(StringComparer.Ordinal);
    }

    public async Task<CertificateRecord> GetOrCreateForOwnerAndLevelAsync(string ownerId, string recipientName, string level, int correctCount, int total)
    {
        await _gate.WaitAsync();
        try
        {
            var existing = _certificates.Values.FirstOrDefault(certificate =>
                string.Equals(certificate.OwnerId, ownerId, StringComparison.Ordinal) &&
                string.Equals(certificate.Level, level, StringComparison.Ordinal));
            if (existing != null) return existing;

            string number;
            do { number = CreateNumber(); }
            while (_certificates.ContainsKey(number));

            var certificate = new CertificateRecord
            {
                Number = number,
                OwnerId = ownerId,
                RecipientName = recipientName,
                Level = level,
                CorrectCount = correctCount,
                Total = total,
                IssuedAtUtc = DateTimeOffset.UtcNow
            };
            _certificates.Add(number, certificate);

            var temporaryPath = _filePath + ".tmp";
            try
            {
                await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    await JsonSerializer.SerializeAsync(stream, _certificates.Values.OrderBy(item => item.IssuedAtUtc).ToList(), JsonOptions);
                File.Move(temporaryPath, _filePath, overwrite: true);
            }
            catch
            {
                _certificates.Remove(number);
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                throw;
            }

            return certificate;
        }
        finally { _gate.Release(); }
    }

    public async Task<CertificateRecord?> FindAsync(string number)
    {
        await _gate.WaitAsync();
        try
        {
            _certificates.TryGetValue(number, out var certificate);
            return certificate;
        }
        finally { _gate.Release(); }
    }

    private static string CreateNumber()
    {
        Span<char> digits = stackalloc char[10];
        digits[0] = (char)('1' + RandomNumberGenerator.GetInt32(9));
        for (var index = 1; index < digits.Length; index++)
            digits[index] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        return new string(digits);
    }
}
