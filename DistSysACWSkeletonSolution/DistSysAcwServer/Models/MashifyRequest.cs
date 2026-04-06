namespace DistSysAcwServer.Models
{
    /// <summary>
    /// Data transfer object for the Protected/Mashify GET request body.
    /// All three fields are hex strings (with dash delimiters) that have been
    /// encrypted using the server's public RSA key.
    /// </summary>
    public class MashifyRequest
    {
        /// <summary>
        /// The message string, encrypted with the server's public RSA key, in hex format.
        /// </summary>
        public required string EncryptedString { get; set; }

        /// <summary>
        /// The AES symmetric key, encrypted with the server's public RSA key, in hex format.
        /// </summary>
        public required string EncryptedSymKey { get; set; }

        /// <summary>
        /// The AES initialization vector, encrypted with the server's public RSA key, in hex format.
        /// </summary>
        public required string EncryptedIV { get; set; }
    }
}