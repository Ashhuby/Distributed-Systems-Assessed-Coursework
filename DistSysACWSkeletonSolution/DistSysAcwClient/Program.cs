using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DistSysAcwClient
{
    /// <summary>
    /// Console-based client for the Distributed Systems ACW server.
    /// Provides an asynchronous command loop that sends HTTP requests
    /// and displays the server's responses.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Base URL for the server. Change this single constant to switch
        /// between local server and the test server.
        /// MUST be set to localhost before submission.
        /// </summary>
        private const string BaseUrl = "http://localhost:53415/";

        // Test server URL (uncomment to test against test server, recomment before submission idiot I know ur going to forget so I added it as a trello task):
        //private const string BaseUrl = "http://distsysacwserver.net.dcs.hull.ac.uk/3185683/";

        /// <summary>
        /// Shared HttpClient instance for all requests.
        /// </summary>
        private static readonly HttpClient Client = new HttpClient();

        /// <summary>
        /// The locally stored username from User Post or User Set.
        /// </summary>
        private static string? StoredUsername;

        /// <summary>
        /// The locally stored API Key from User Post or User Set.
        /// </summary>
        private static string? StoredApiKey;

        /// <summary>
        /// The server's RSA public key XML, stored from Protected Get PublicKey.
        /// </summary>
        private static string? StoredPublicKey;

        /// <summary>
        /// Entry point. Runs the async command loop.
        /// </summary>
        static void Main(string[] args)
        {
            Client.BaseAddress = new Uri(BaseUrl);
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            RunAsync().Wait();
        }

        /// <summary>
        /// Main async command loop. Displays prompts, reads user input,
        /// dispatches commands, and displays results.
        /// </summary>
        private static async Task RunAsync()
        {
            Console.WriteLine("Hello. What would you like to do?");

            while (true)
            {
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                // Exit command
                if (input.Equals("Exit", StringComparison.Ordinal))
                {
                    return;
                }

                Console.Clear();
                Console.WriteLine("...please wait...");

                try
                {
                    await DispatchCommand(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Console.WriteLine("What would you like to do next?");
            }
        }

        /// <summary>
        /// Parses the user input and dispatches it to the appropriate handler method.
        /// </summary>
        /// <param name="input">The raw command string from the console.</param>
        private static async Task DispatchCommand(string input)
        {
            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                Console.WriteLine("Unknown command.");
                return;
            }

            string category = parts[0];
            string action = parts[1];

            switch (category)
            {
                case "TalkBack":
                    await HandleTalkBack(action, parts);
                    break;
                case "User":
                    await HandleUser(action, parts);
                    break;
                case "Protected":
                    await HandleProtected(action, parts, input);
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        #region TalkBack Commands

        /// <summary>
        /// Handles TalkBack Hello and TalkBack Sort commands.
        /// </summary>
        private static async Task HandleTalkBack(string action, string[] parts)
        {
            switch (action)
            {
                case "Hello":
                    await TalkBackHello();
                    break;
                case "Sort":
                    await TalkBackSort(parts);
                    break;
                default:
                    Console.WriteLine("Unknown TalkBack command.");
                    break;
            }
        }

        /// <summary>
        /// GET api/talkback/hello
        /// Prints the response string to the console.
        /// </summary>
        private static async Task TalkBackHello()
        {
            string response = await Client.GetStringAsync("api/talkback/hello");
            Console.WriteLine(response);
        }

        /// <summary>
        /// GET api/talkback/sort?integers=X&amp;integers=Y&amp;integers=Z
        /// Parses the integer array from the command (e.g. "TalkBack Sort [6,1,8,4,3]"),
        /// builds the query string, and prints the response.
        /// </summary>
        private static async Task TalkBackSort(string[] parts)
        {
            if (parts.Length < 3)
            {
                Console.WriteLine("[]");
                return;
            }

            // Join everything after "TalkBack Sort" and strip brackets
            string arrayPart = string.Join(" ", parts.Skip(2));
            arrayPart = arrayPart.Trim('[', ']');

            if (string.IsNullOrWhiteSpace(arrayPart))
            {
                Console.WriteLine("[]");
                return;
            }

            string[] values = arrayPart.Split(',', StringSplitOptions.RemoveEmptyEntries);
            string queryString = string.Join("&", values.Select(v => "integers=" + v.Trim()));

            string response = await Client.GetStringAsync("api/talkback/sort?" + queryString);
            Console.WriteLine(response);
        }

        #endregion

        #region User Commands

        /// <summary>
        /// Handles User Get, User Post, User Set, User Delete, and User Role commands.
        /// </summary>
        private static async Task HandleUser(string action, string[] parts)
        {
            switch (action)
            {
                case "Get":
                    if (parts.Length >= 3)
                        await UserGet(parts[2]);
                    else
                        Console.WriteLine("Usage: User Get <name>");
                    break;
                case "Post":
                    if (parts.Length >= 3)
                        await UserPost(parts[2]);
                    else
                        Console.WriteLine("Usage: User Post <name>");
                    break;
                case "Set":
                    if (parts.Length >= 4)
                        UserSet(parts[2], parts[3]);
                    else
                        Console.WriteLine("Usage: User Set <name> <apikey>");
                    break;
                case "Delete":
                    await UserDelete();
                    break;
                case "Role":
                    if (parts.Length >= 4)
                        await UserRole(parts[2], parts[3]);
                    else
                        Console.WriteLine("Usage: User Role <username> <role>");
                    break;
                default:
                    Console.WriteLine("Unknown User command.");
                    break;
            }
        }

        /// <summary>
        /// GET api/user/new?username=[name]
        /// Prints the response string.
        /// </summary>
        private static async Task UserGet(string name)
        {
            string response = await Client.GetStringAsync("api/user/new?username=" + Uri.EscapeDataString(name));
            Console.WriteLine(response);
        }

        /// <summary>
        /// POST api/user/new with a JSON string in the body.
        /// On success (200), stores the API Key and username, prints "Got API Key".
        /// On failure, prints the response string.
        /// </summary>
        private static async Task UserPost(string name)
        {
            // Send the username as a raw JSON string (e.g. "UserOne" with quotes)
            string jsonBody = JsonSerializer.Serialize(name);
            StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Client.PostAsync("api/user/new", content);
            string responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // The response is the API Key (may be wrapped in quotes from JSON serialization)
                string apiKey = responseString.Trim('"');
                StoredApiKey = apiKey;
                StoredUsername = name;
                Console.WriteLine("Got API Key");
            }
            else
            {
                Console.WriteLine(responseString);
            }
        }

        /// <summary>
        /// User Set [name] [apikey] — stores the username and API Key locally.
        /// No network request is made.
        /// </summary>
        private static void UserSet(string name, string apiKey)
        {
            StoredUsername = name;
            StoredApiKey = apiKey;
            Console.WriteLine("Stored");
        }

        /// <summary>
        /// DELETE api/user/removeuser?username=[storedUsername]
        /// with ApiKey in the header. Prints "True" or "False".
        /// </summary>
        private static async Task UserDelete()
        {
            if (!HasCredentials())
            {
                return;
            }

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Delete,
                "api/user/removeuser?username=" + Uri.EscapeDataString(StoredUsername!));
            request.Headers.Add("ApiKey", StoredApiKey);

            HttpResponseMessage response = await Client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
        }

        /// <summary>
        /// POST api/user/changerole with ApiKey header and JSON body
        /// containing {"username":"...","role":"..."}.
        /// Prints the response string.
        /// </summary>
        private static async Task UserRole(string username, string role)
        {
            if (!HasCredentials())
            {
                return;
            }

            var bodyObject = new { username = username, role = role };
            string jsonBody = JsonSerializer.Serialize(bodyObject);
            StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "api/user/changerole");
            request.Headers.Add("ApiKey", StoredApiKey);
            request.Content = content;

            HttpResponseMessage response = await Client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
        }

        #endregion

        #region Protected Commands

        /// <summary>
        /// Handles Protected Hello, SHA1, SHA256, Get PublicKey, Sign, and Mashify commands.
        /// </summary>
        private static async Task HandleProtected(string action, string[] parts, string fullInput)
        {
            switch (action)
            {
                case "Hello":
                    await ProtectedHello();
                    break;
                case "SHA1":
                    if (parts.Length >= 3)
                        await ProtectedSHA1(GetMessageFromParts(parts, 2));
                    else
                        Console.WriteLine("Usage: Protected SHA1 <message>");
                    break;
                case "SHA256":
                    if (parts.Length >= 3)
                        await ProtectedSHA256(GetMessageFromParts(parts, 2));
                    else
                        Console.WriteLine("Usage: Protected SHA256 <message>");
                    break;
                case "Get":
                    if (parts.Length >= 3 && parts[2] == "PublicKey")
                        await ProtectedGetPublicKey();
                    else
                        Console.WriteLine("Unknown Protected Get command.");
                    break;
                case "Sign":
                    if (parts.Length >= 3)
                        await ProtectedSign(GetMessageFromParts(parts, 2));
                    else
                        Console.WriteLine("Usage: Protected Sign <message>");
                    break;
                case "Mashify":
                    if (parts.Length >= 3)
                        await ProtectedMashify(GetMessageFromParts(parts, 2));
                    else
                        Console.WriteLine("Usage: Protected Mashify <string>");
                    break;
                default:
                    Console.WriteLine("Unknown Protected command.");
                    break;
            }
        }

        /// <summary>
        /// GET api/protected/hello with ApiKey header.
        /// Prints the response string.
        /// </summary>
        private static async Task ProtectedHello()
        {
            if (!HasCredentials())
            {
                return;
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/protected/hello");
            request.Headers.Add("ApiKey", StoredApiKey);

            HttpResponseMessage response = await Client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
        }

        /// <summary>
        /// GET api/protected/sha1?message=[message] with ApiKey header.
        /// Prints the response string.
        /// </summary>
        private static async Task ProtectedSHA1(string message)
        {
            if (!HasCredentials())
            {
                return;
            }

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/protected/sha1?message=" + Uri.EscapeDataString(message));
            request.Headers.Add("ApiKey", StoredApiKey);

            HttpResponseMessage response = await Client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
        }

        /// <summary>
        /// GET api/protected/sha256?message=[message] with ApiKey header.
        /// Prints the response string.
        /// </summary>
        private static async Task ProtectedSHA256(string message)
        {
            if (!HasCredentials())
            {
                return;
            }

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/protected/sha256?message=" + Uri.EscapeDataString(message));
            request.Headers.Add("ApiKey", StoredApiKey);

            HttpResponseMessage response = await Client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
        }

        /// <summary>
        /// GET api/protected/getpublickey with ApiKey header.
        /// Stores the returned public key XML.
        /// Prints "Got Public Key" on success or "Couldn't Get the Public Key" on failure.
        /// </summary>
        private static async Task ProtectedGetPublicKey()
        {
            if (!HasCredentials())
            {
                return;
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/protected/getpublickey");
            request.Headers.Add("ApiKey", StoredApiKey);

            HttpResponseMessage response = await Client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();

                // Strip surrounding quotes if the response is JSON-serialized
                string publicKey = responseString.Trim('"');

                if (!string.IsNullOrEmpty(publicKey) && publicKey.Contains("<RSAKeyValue>"))
                {
                    StoredPublicKey = publicKey;
                    Console.WriteLine("Got Public Key");
                }
                else
                {
                    Console.WriteLine("Couldn't Get the Public Key");
                }
            }
            else
            {
                Console.WriteLine("Couldn't Get the Public Key");
            }
        }

        /// <summary>
        /// GET api/protected/sign?message=[message] with ApiKey header.
        /// Verifies the returned signature using the stored public key.
        /// Prints "Message was successfully signed", "Message was not successfully signed",
        /// or "Client doesn't yet have the public key".
        /// </summary>
        private static async Task ProtectedSign(string message)
        {
            if (!HasCredentials())
            {
                return;
            }

            if (string.IsNullOrEmpty(StoredPublicKey))
            {
                Console.WriteLine("Client doesn't yet have the public key");
                return;
            }

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/protected/sign?message=" + Uri.EscapeDataString(message));
            request.Headers.Add("ApiKey", StoredApiKey);

            HttpResponseMessage response = await Client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            // Strip surrounding quotes if JSON-serialized
            string hexSignature = responseString.Trim('"');

            try
            {
                // Convert the hex signature (with dashes) to bytes
                byte[] signatureBytes = HexStringToByteArray(hexSignature);

                // The message must be ASCII encoded and not modified
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);

                // Verify using the server's public key
                using (RSA rsa = RSA.Create())
                {
                    rsa.FromXmlString(StoredPublicKey);

                    bool isValid = rsa.VerifyData(
                        messageBytes,
                        signatureBytes,
                        HashAlgorithmName.SHA1,
                        RSASignaturePadding.Pkcs1);

                    Console.WriteLine(isValid
                        ? "Message was successfully signed"
                        : "Message was not successfully signed");
                }
            }
            catch
            {
                Console.WriteLine("Message was not successfully signed");
            }
        }

        /// <summary>
        /// GET api/protected/mashify with ApiKey header and JSON body containing
        /// three RSA-encrypted hex strings (message, AES key, AES IV).
        /// Decrypts the response using the local AES key and IV.
        /// Prints the decrypted mashified string or "An error occurred!".
        /// </summary>
        private static async Task ProtectedMashify(string message)
        {
            if (!HasCredentials())
            {
                return;
            }

            if (string.IsNullOrEmpty(StoredPublicKey))
            {
                Console.WriteLine("Client doesn't yet have the public key");
                return;
            }

            try
            {
                // Generate a new AES key and IV
                byte[] aesKey;
                byte[] aesIv;

                using (Aes aes = Aes.Create())
                {
                    aes.GenerateKey();
                    aes.GenerateIV();
                    aesKey = aes.Key;
                    aesIv = aes.IV;
                }

                // Encrypt the message, AES key, and AES IV using the server's public RSA key
                using (RSA rsa = RSA.Create())
                {
                    rsa.FromXmlString(StoredPublicKey);

                    byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                    byte[] encryptedMessage = rsa.Encrypt(messageBytes, RSAEncryptionPadding.OaepSHA1);
                    byte[] encryptedKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA1);
                    byte[] encryptedIv = rsa.Encrypt(aesIv, RSAEncryptionPadding.OaepSHA1);

                    // Convert to hex with dashes
                    string hexMessage = BitConverter.ToString(encryptedMessage);
                    string hexKey = BitConverter.ToString(encryptedKey);
                    string hexIv = BitConverter.ToString(encryptedIv);

                    // Build the JSON body
                    var bodyObject = new
                    {
                        EncryptedString = hexMessage,
                        EncryptedSymKey = hexKey,
                        EncryptedIV = hexIv
                    };

                    string jsonBody = JsonSerializer.Serialize(bodyObject);
                    StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    // Send as GET with a body
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/protected/mashify");
                    request.Headers.Add("ApiKey", StoredApiKey);
                    request.Content = content;

                    HttpResponseMessage response = await Client.SendAsync(request);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("An error occurred!");
                        return;
                    }

                    // Strip surrounding quotes if JSON-serialized
                    string hexResult = responseString.Trim('"');

                    // Convert hex (with dashes) back to bytes
                    byte[] encryptedResult = HexStringToByteArray(hexResult);

                    // Decrypt using the AES key and IV we generated
                    string decryptedString = AesDecrypt(encryptedResult, aesKey, aesIv);
                    Console.WriteLine(decryptedString);
                }
            }
            catch
            {
                Console.WriteLine("An error occurred!");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks whether the client has stored credentials (username and API key).
        /// Prints the required message if credentials are missing.
        /// </summary>
        /// <returns>True if credentials are available; otherwise false.</returns>
        private static bool HasCredentials()
        {
            if (string.IsNullOrEmpty(StoredApiKey) || string.IsNullOrEmpty(StoredUsername))
            {
                Console.WriteLine("You need to do a User Post or User Set first");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Joins all parts from a given start index into a single string.
        /// Used to reconstruct messages that contain spaces.
        /// </summary>
        /// <param name="parts">The split command parts.</param>
        /// <param name="startIndex">The index from which to start joining.</param>
        /// <returns>The joined message string.</returns>
        private static string GetMessageFromParts(string[] parts, int startIndex)
        {
            return string.Join(" ", parts.Skip(startIndex));
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
        /// Decrypts data using AES with the provided key and initialization vector.
        /// </summary>
        /// <param name="encryptedData">The encrypted bytes.</param>
        /// <param name="key">The AES key.</param>
        /// <param name="iv">The AES initialization vector.</param>
        /// <returns>The decrypted string.</returns>
        private static string AesDecrypt(byte[] encryptedData, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    using (MemoryStream ms = new MemoryStream(encryptedData))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader reader = new StreamReader(cs))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}