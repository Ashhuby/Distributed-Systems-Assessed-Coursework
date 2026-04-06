using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DistSysAcwServer.Models;
using DistSysAcwServer.Services;
using DistSysAcwServer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistSysAcwServer.Controllers
{
    /// <summary>
    /// Handles protected API requests that require authentication.
    /// Provides Hello, SHA1, SHA256, GetPublicKey, Sign, and Mashify endpoints.
    /// </summary>
    [Authorize(Roles = "Admin,User")]
    public class ProtectedController : BaseController
    {
        private readonly RsaKeyService _rsaKeyService;

        /// <summary>
        /// Constructs a Protected controller with dependency-injected services.
        /// </summary>
        /// <param name="dbcontext">The Entity Framework database context.</param>
        /// <param name="error">The shared error object for the request pipeline.</param>
        /// <param name="rsaKeyService">The singleton RSA key service.</param>
        public ProtectedController(
            UserContext dbcontext,
            SharedError error,
            RsaKeyService rsaKeyService)
            : base(dbcontext, error)
        {
            _rsaKeyService = rsaKeyService;
        }

        #region Task9

        /// <summary>
        /// GET api/protected/hello
        /// Returns "Hello [username]" for the authenticated user.
        /// Requires User or Admin role.
        /// </summary>
        [HttpGet]
        public IActionResult Hello()
        {
            string username = User.Identity?.Name ?? string.Empty;

            // Log the request
            string? apiKey = Request.Headers["ApiKey"].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
            {
                UserDatabaseAccess.AddLog(apiKey, "User requested /Protected/Hello", DbContext);
            }

            return Ok("Hello " + username);
        }

        /// <summary>
        /// GET api/protected/sha1?message=hello
        /// Computes the SHA1 hash of the given message and returns it as an
        /// uppercase hexadecimal string with no delimiters.
        /// Requires User or Admin role.
        /// </summary>
        /// <param name="message">The string to hash.</param>
        [HttpGet]
        public IActionResult SHA1([FromQuery] string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Bad Request");
            }

            // Log the request
            string? apiKey = Request.Headers["ApiKey"].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
            {
                UserDatabaseAccess.AddLog(apiKey, "User requested /Protected/SHA1", DbContext);
            }

            byte[] messageBytes = Encoding.ASCII.GetBytes(message);
            byte[] hashBytes;

            using (System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create())
            {
                hashBytes = sha1.ComputeHash(messageBytes);
            }

            // Convert to uppercase hex with no delimiters
            string hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            return Ok(hexHash);
        }

        /// <summary>
        /// GET api/protected/sha256?message=hello
        /// Computes the SHA256 hash of the given message and returns it as an
        /// uppercase hexadecimal string with no delimiters.
        /// Requires User or Admin role.
        /// </summary>
        /// <param name="message">The string to hash.</param>
        [HttpGet]
        public IActionResult SHA256([FromQuery] string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Bad Request");
            }

            // Log the request
            string? apiKey = Request.Headers["ApiKey"].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
            {
                UserDatabaseAccess.AddLog(apiKey, "User requested /Protected/SHA256", DbContext);
            }

            byte[] messageBytes = Encoding.ASCII.GetBytes(message);
            byte[] hashBytes;

            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                hashBytes = sha256.ComputeHash(messageBytes);
            }

            // Convert to uppercase hex with no delimiters
            string hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            return Ok(hexHash);
        }

        #endregion

        #region Task11

        /// <summary>
        /// GET api/protected/getpublickey
        /// Returns the server's RSA public key as an XML string.
        /// Requires User or Admin role.
        /// </summary>
        [HttpGet]
        public IActionResult GetPublicKey()
        {
            // Log the request
            string? apiKey = Request.Headers["ApiKey"].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
            {
                UserDatabaseAccess.AddLog(apiKey, "User requested /Protected/GetPublicKey", DbContext);
            }

            string publicKeyXml = _rsaKeyService.GetPublicKeyXml();
            return Ok(publicKeyXml);
        }

        #endregion

        #region Task12

        /// <summary>
        /// GET api/protected/sign?message=Hello
        /// Digitally signs the message using the server's private RSA key with SHA1.
        /// Returns the signature as a hexadecimal string WITH dash delimiters.
        /// The message is ASCII encoded and not modified prior to signing.
        /// Requires User or Admin role.
        /// </summary>
        /// <param name="message">The string to sign.</param>
        [HttpGet]
        public IActionResult Sign([FromQuery] string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Bad Request");
            }

            // Log the request
            string? apiKey = Request.Headers["ApiKey"].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
            {
                UserDatabaseAccess.AddLog(apiKey, "User requested /Protected/Sign", DbContext);
            }

            byte[] messageBytes = Encoding.ASCII.GetBytes(message);
            byte[] signatureBytes = _rsaKeyService.SignData(messageBytes);

            // Convert to hex WITH dashes (BitConverter.ToString default format)
            string hexSignature = BitConverter.ToString(signatureBytes);
            return Ok(hexSignature);
        }

        #endregion

        #region Task14

        /// <summary>
        /// GET api/protected/mashify
        /// Accepts three RSA-encrypted hex strings in the body (message, AES key, AES IV).
        /// Decrypts all three, mashifies the message, encrypts the result with the client's
        /// AES key and IV, and returns the encrypted mashified string as hex with dashes.
        /// Requires Admin role only.
        /// </summary>
        /// <param name="request">The MashifyRequest containing the three encrypted hex strings.</param>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Mashify([FromBody] MashifyRequest? request)
        {
            try
            {
                if (request == null ||
                    string.IsNullOrEmpty(request.EncryptedString) ||
                    string.IsNullOrEmpty(request.EncryptedSymKey) ||
                    string.IsNullOrEmpty(request.EncryptedIV))
                {
                    return BadRequest("Bad Request");
                }

                // Log the request (without compromising encrypted data)
                string? apiKey = Request.Headers["ApiKey"].FirstOrDefault();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    UserDatabaseAccess.AddLog(apiKey, "User requested /Protected/Mashify", DbContext);
                }

                // Convert hex strings (with dashes) back to byte arrays
                byte[] encryptedMessageBytes = HexStringToByteArray(request.EncryptedString);
                byte[] encryptedKeyBytes = HexStringToByteArray(request.EncryptedSymKey);
                byte[] encryptedIvBytes = HexStringToByteArray(request.EncryptedIV);

                // Decrypt all three using the server's private RSA key
                byte[] messageBytes = _rsaKeyService.Decrypt(encryptedMessageBytes);
                byte[] aesKeyBytes = _rsaKeyService.Decrypt(encryptedKeyBytes);
                byte[] aesIvBytes = _rsaKeyService.Decrypt(encryptedIvBytes);

                // Convert the decrypted message bytes to a string
                string originalMessage = Encoding.ASCII.GetString(messageBytes);

                // Mashify the string
                string mashified = MashifyString(originalMessage);

                // Encrypt the mashified string using the client's AES key and IV
                byte[] mashifiedBytes = Encoding.ASCII.GetBytes(mashified);
                byte[] encryptedResult = AesEncrypt(mashifiedBytes, aesKeyBytes, aesIvBytes);

                // Return as hex with dashes
                string hexResult = BitConverter.ToString(encryptedResult);
                return Ok(hexResult);
            }
            catch
            {
                return BadRequest("Bad Request");
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Mashifies a string by converting all vowels (aeiouAEIOU) to uppercase 'X'
        /// and then reversing the entire string.
        /// </summary>
        /// <param name="input">The string to mashify.</param>
        /// <returns>The mashified string.</returns>
        private static string MashifyString(string input)
        {
            // Step 1: Convert all vowels to uppercase 'X'
            char[] chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if ("aeiouAEIOU".Contains(chars[i]))
                {
                    chars[i] = 'X';
                }
            }

            // Step 2: Reverse the string
            Array.Reverse(chars);
            return new string(chars);
        }

        /// <summary>
        /// Converts a hex string with dash delimiters (e.g. "7B-05-B3") to a byte array.
        /// </summary>
        /// <param name="hex">The hex string to convert.</param>
        /// <returns>The corresponding byte array.</returns>
        private static byte[] HexStringToByteArray(string hex)
        {
            string[] hexValues = hex.Split('-');
            byte[] bytes = new byte[hexValues.Length];

            for (int i = 0; i < hexValues.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexValues[i], 16);
            }

            return bytes;
        }

        /// <summary>
        /// Encrypts data using AES with the provided key and initialization vector.
        /// </summary>
        /// <param name="data">The plaintext bytes to encrypt.</param>
        /// <param name="key">The AES key.</param>
        /// <param name="iv">The AES initialization vector.</param>
        /// <returns>The encrypted bytes.</returns>
        private static byte[] AesEncrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
        }

        #endregion
    }
}