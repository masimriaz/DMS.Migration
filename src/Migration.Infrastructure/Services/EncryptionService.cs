using Microsoft.AspNetCore.DataProtection;
using DMS.Migration.Application.Common.Abstractions;

namespace DMS.Migration.Infrastructure.Services;

/// <summary>
/// Encryption service using ASP.NET Core Data Protection
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider protectionProvider)
    {
        _protector = protectionProvider.CreateProtector("DMS.Migration.Secrets");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        return _protector.Protect(plainText);
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrWhiteSpace(encryptedText))
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));

        return _protector.Unprotect(encryptedText);
    }
}
