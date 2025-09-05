using System.Security.Claims;

namespace Synaptic.NET.Core;

public interface IEncryptionService
{
    string Encrypt(string data, ClaimsIdentity identity);
    string Decrypt(string encryptedData, ClaimsIdentity identity);
    bool IsContentEncrypted(string content);
}
