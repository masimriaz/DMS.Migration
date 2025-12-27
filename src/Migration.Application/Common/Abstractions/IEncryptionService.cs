namespace DMS.Migration.Application.Common.Abstractions;

/// <summary>
/// Service for encrypting/decrypting sensitive data (credentials, tokens, etc.)
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plain text value
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted value
    /// </summary>
    string Decrypt(string encryptedText);
}
