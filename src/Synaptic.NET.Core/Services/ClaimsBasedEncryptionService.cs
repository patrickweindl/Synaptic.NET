using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Synaptic.NET.Domain.Abstractions.Services;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Core.Services;

public class ClaimsBasedEncryptionService : IEncryptionService
{
    private const string EncryptionHeader = "aesgcm:v1";
    private readonly IConfiguration _configuration;
    public ClaimsBasedEncryptionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private byte[] Build256BitKey(ClaimsIdentity claims)
    {
        if (string.IsNullOrEmpty(_configuration["Encryption:MasterKey"]))
        {
            throw new InvalidOperationException("Encryption:MasterKey is not set in configuration through Environment Variables or appsettings.json.");
        }
        byte[] currentUserBytes = Encoding.UTF8.GetBytes(claims.ToUserIdentifier());
        byte[] masterKeyBytes = Encoding.UTF8.GetBytes(_configuration["Encryption:MasterKey"]!);

        List<byte> keyBytes = new();
        keyBytes.AddRange(currentUserBytes);
        keyBytes.AddRange(masterKeyBytes);
        return keyBytes.Take(256 / 8).ToArray();
    }

    public bool IsContentEncrypted(string content)
    {
        return content.StartsWith(EncryptionHeader);
    }

    public string Encrypt(string data, ClaimsIdentity claims)
    {
        byte[] iv = new byte[16];
        byte[] array;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Build256BitKey(claims);
            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new())
            {
                using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new(cryptoStream))
                    {
                        streamWriter.Write(data);
                    }

                    array = memoryStream.ToArray();
                }
            }
        }

        return EncryptionHeader + Convert.ToBase64String(array);
    }

    public string Decrypt(string encryptedData, ClaimsIdentity claims)
    {
        if (!encryptedData.StartsWith(EncryptionHeader))
        {
            Log.Logger.Warning("[Encryption] Decrypt called with data that does not start with the expected header. Returning the original data without decryption.");
            return encryptedData;
        }

        string removedHeaderData = encryptedData.Substring(EncryptionHeader.Length);
        byte[] iv = new byte[16];
        byte[] buffer = Convert.FromBase64String(removedHeaderData);

        if (string.IsNullOrEmpty(_configuration["Encryption:MasterKey"]))
        {
            throw new InvalidOperationException("Encryption:MasterKey is not set in configuration through Environment Variables or appsettings.json.");
        }
        using (Aes aes = Aes.Create())
        {
            aes.Key = Build256BitKey(claims);
            aes.IV = iv;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new(buffer))
            {
                using (CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader streamReader = new(cryptoStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }
}
