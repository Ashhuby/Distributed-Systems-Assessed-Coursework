using System.Security.Cryptography;

namespace DistSysAcwServer.Services
{
    /// <summary>
    /// Singleton service that manages the server's RSA key pair.
    /// The same key pair is shared across all threads and requests.
    /// A new key pair is generated each time the server starts.
    /// The private key is stored securely using the Windows Machine Key Store.
    /// </summary>
    public class RsaKeyService
    {
        /// <summary>
        /// The RSA cryptographic provider shared across the application.
        /// </summary>
        public RSA RsaProvider { get; }

        /// <summary>
        /// Initialises the RSA key service with a new key pair.
        /// Uses CspParameters with MachineKeyStore for secure key storage on Windows.
        /// </summary>
        public RsaKeyService()
        {
            CspParameters cspParams = new CspParameters
            {
                Flags = CspProviderFlags.UseMachineKeyStore
            };

            RsaProvider = new RSACryptoServiceProvider(cspParams);
        }

        /// <summary>
        /// Returns the server's public RSA key as an XML string.
        /// Does NOT include the private key (false parameter).
        /// </summary>
        /// <returns>XML string containing only the public key.</returns>
        public string GetPublicKeyXml()
        {
            return RsaProvider.ToXmlString(false);
        }

        /// <summary>
        /// Signs the given data using the server's private RSA key with SHA1 hash algorithm.
        /// </summary>
        /// <param name="data">The raw bytes to sign.</param>
        /// <returns>The digital signature as a byte array.</returns>
        public byte[] SignData(byte[] data)
        {
            return RsaProvider.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }

        /// <summary>
        /// Decrypts data that was encrypted with the server's public RSA key.
        /// Uses OaepSHA1 padding as specified in the coursework.
        /// </summary>
        /// <param name="encryptedData">The encrypted bytes to decrypt.</param>
        /// <returns>The decrypted bytes.</returns>
        public byte[] Decrypt(byte[] encryptedData)
        {
            return RsaProvider.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA1);
        }
    }
}