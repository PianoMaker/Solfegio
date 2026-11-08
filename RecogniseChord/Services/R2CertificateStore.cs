using System.Security.Cryptography;
using System.Text.Json;
using RecogniseChord.Models;

namespace RecogniseChord.Services;

public sealed class R2CertificateStore : ICertificateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly R2JsonObjectStore _objects;
    private readonly string _legacyFilePath;
    private readonly SemaphoreSlim _migrationGate = new(1, 1);
    private bool _legacyDataMigrated;

    public R2CertificateStore(
        R2JsonObjectStore objects,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _objects = objects;
        var configuredPath = configuration["Certificates:StoragePath"];
        _legacyFilePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "certificates.json")
            : Path.GetFullPath(Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath));
    }

    public async Task<CertificateRecord> GetOrCreateForOwnerAndLevelAsync(
        string ownerId,
        string recipientName,
        string level,
        int correctCount,
        int total)
    {
        await EnsureLegacyDataMigratedAsync();
        var existing = (await _objects.ListAsync<CertificateRecord>("certificates/"))
            .FirstOrDefault(certificate =>
                string.Equals(certificate.OwnerId, ownerId, StringComparison.Ordinal) &&
                string.Equals(certificate.Level, level, StringComparison.Ordinal));

        string number;
        if (existing is not null)
        {
            number = existing.Number;
        }
        else
        {
            do
            {
                number = CreateNumber();
            }
            while (await _objects.GetAsync<CertificateRecord>($"certificates/{number}.json") is not null);
        }

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

        await _objects.PutAsync($"certificates/{number}.json", certificate);
        return certificate;
    }

    public async Task<CertificateRecord?> FindAsync(string number)
    {
        await EnsureLegacyDataMigratedAsync();
        return await _objects.GetAsync<CertificateRecord>($"certificates/{number}.json");
    }

    public async Task<IReadOnlyList<CertificateRecord>> ListAsync()
    {
        await EnsureLegacyDataMigratedAsync();
        var certificates = await _objects.ListAsync<CertificateRecord>("certificates/");
        return certificates.OrderByDescending(certificate => certificate.IssuedAtUtc).ToArray();
    }

    private async Task EnsureLegacyDataMigratedAsync()
    {
        if (_legacyDataMigrated)
            return;

        await _migrationGate.WaitAsync();
        try
        {
            if (_legacyDataMigrated)
                return;

            if (File.Exists(_legacyFilePath))
            {
                var json = await File.ReadAllTextAsync(_legacyFilePath);
                var certificates = JsonSerializer.Deserialize<List<CertificateRecord>>(json, JsonOptions) ?? [];
                foreach (var certificate in certificates.Where(item => !string.IsNullOrWhiteSpace(item.Number)))
                    await _objects.PutIfMissingAsync($"certificates/{certificate.Number}.json", certificate);
            }

            _legacyDataMigrated = true;
        }
        finally
        {
            _migrationGate.Release();
        }
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
