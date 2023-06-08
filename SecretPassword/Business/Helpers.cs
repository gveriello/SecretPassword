using Entities;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Business
{
    public static class Helpers
    {

#if DEBUG
        static string EnvironmentKey { get { return "DEV"; } }
#else
        static string EnvironmentKey { get { return "PROD"; } }
#endif

        public static string AppDataPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"SecretPassword{EnvironmentKey}"); } }

        public static void RegisterCustomUrlProtocol()
        {
            try
            {
                var KeyTest = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("Classes", true);
                RegistryKey key = null;
                key = KeyTest.OpenSubKey($"sp{EnvironmentKey}");
                if (key != null)
                    return;

                key = KeyTest.CreateSubKey($"sp{EnvironmentKey}");
                key.SetValue("URL Protocol", $"sp{EnvironmentKey}");
                string path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                key.CreateSubKey(@"shell\open\command").SetValue("", "\"" + path + "\"");
                //key.CreateSubKey(@"shell\open\command").SetValue("", "\"" + applicationPath + "\" \"%1\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }

        static string DatabaseGroupsPath { get { return Path.Combine(AppDataPath, "dbag.sp"); } }
        static string DatabaseCredentialsPath { get { return Path.Combine(AppDataPath, "dbac.sp"); } }
        static string UsersSalt { get; set; }

        public static void Lock()
        {
            UsersSalt = string.Empty;
        }

        public static string GetCredentialCripted(Credential credential)
        {
            if (credential != null)
            {
                credential.ShowPassword = true;
                string salt = Path.GetRandomFileName().Replace(".", "");

                string credentialSerialized = JsonConvert.SerializeObject(credential);
                string credentialCrypted = credentialSerialized.Encrypt(salt);
                StreamToShare streamToShare = new StreamToShare()
                {
                    Stream = credentialCrypted,
                    Salt = salt
                };
                credential.ShowPassword = false;
                string streamJson = JsonConvert.SerializeObject(streamToShare);
                return streamJson.ToBase64();
            }
            throw new Exception("Impossibile creare lo stream di condivisione.");
        }

        public static bool ConvalidateSalt(bool isRequired = false)
        {
            while(true)
            {
                string saltFromUser = AskSalt(isRequired);
                if (UsersSalt == saltFromUser)
                    return true;

                if (isRequired)
                    continue;

                return false;
            }
        }

        public static void CheckIfExistSalt()
        {
            //VLN87O41I7J3zuMNSYHAUA==
            //VkxOODdPNDFJN0ozenVNTlNZSEFVQT09
            RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\SP{EnvironmentKey}", true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey($"SOFTWARE\\SP{EnvironmentKey}");
                key.SetValue("SPSalt", AskSalt(isRequired: true, isFirstAccess: true).ToBase64());
            }
            if (key != null)
            {
                if (string.IsNullOrEmpty(key.GetValue("SPSalt")?.ToString()))
                    key.SetValue("SPSalt", AskSalt(isRequired: true, isFirstAccess: true).ToBase64());//.Encrypt(Path.GetRandomFileName().Replace(".", "")));
            }
            UsersSalt = key.GetValue("SPSalt").ToString().FromBase64();
            key.Close();
        }

        public static void ChangeSalt(string newSalt)
        {
            if (string.IsNullOrEmpty(newSalt))
                return;

            UsersSalt = newSalt.ToBase64();
            RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\SP{EnvironmentKey}", true);
            if (key != null)
                key.SetValue("SPSalt", UsersSalt);
            key.Close();
        }

        public static string GetOrAskSalt()
        {
            if (!string.IsNullOrEmpty(UsersSalt))
                return UsersSalt;

            return AskSalt();
        }

        private static string AskQuestionToUser(string message, string title = "SecretPassword", bool isRequired = false, string messageToAddWhenFail = "Il campo è obbligatorio.")
        {
            bool isAsked = false;
            while(true)
            {
                string messageToShow = message;
                if (isAsked)
                    messageToShow = $"{messageToShow}{Environment.NewLine}{messageToAddWhenFail}";

                string input = AskQuestionToUser(messageToShow, title);

                if (!isRequired)
                    return input;

                if (isRequired)
                    if (!string.IsNullOrEmpty(input))
                        return input;

                isAsked = true;
            }
        }

        private static string AskQuestionToUser(string message, string title = "SecretPassword")
        {
            return Interaction.InputBox(message, title);
        }

        public static string AskSalt(bool isRequired = false, bool isFirstAccess = false)
        {
            bool isAsked = false;
            return AskQuestionToUser($"La password che inserirai verrà utilizzata per criptare/decriptare le tue informazioni in totale sicurezza.{Environment.NewLine}" +
                        $"Ti verrà chiesta ogni volta che verrà effettuata un' operazione di sicurezza.", "Inserisci la password", isRequired, $"{Environment.NewLine}La password è obbligatoria.");
        }

        public static string AskStream()
        {
            return Interaction.InputBox("Inserisci lo stream delle credenziali da importare.");
        }

        public static void CheckBaseFolder()
        {
            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);
        }

        public static void CreateDatabasesIfNotExists()
        {
            CheckBaseFolder();

            if (!File.Exists(DatabaseGroupsPath))
            {
                File.Create(DatabaseGroupsPath);
            }
            if (!File.Exists(DatabaseCredentialsPath))
            {
                File.Create(DatabaseCredentialsPath);
            }
        }

        public static void SaveGroups(string groups)
        {
            CreateDatabasesIfNotExists();
            CheckIfExistSalt();

            File.WriteAllText(DatabaseGroupsPath, groups.Encrypt(UsersSalt));
        }

        public static string ReadGroups()
        {
            CreateDatabasesIfNotExists();
            CheckIfExistSalt();

            return File.ReadAllText(DatabaseGroupsPath).Decrypt(UsersSalt);
        }
        public static void SaveCredentials(string credentials)
        {
            CreateDatabasesIfNotExists();
            CheckIfExistSalt();

            File.WriteAllText(DatabaseCredentialsPath, credentials.Encrypt(UsersSalt));
        }

        public static string ReadCredentials()
        {
            CreateDatabasesIfNotExists();
            CheckIfExistSalt();

            return File.ReadAllText(DatabaseCredentialsPath).Decrypt(UsersSalt);
        }

        public static string ReadBackup(string fileName, string userSalt)
        {
            CreateDatabasesIfNotExists();
            CheckIfExistSalt();

            return File.ReadAllText(fileName).Decrypt(userSalt);
        }
    }

    public static class Crypto
    {
        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        public static string Encrypt(this string plainText, string passPhrase)
        {
            byte[] iv = new byte[16] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
            SHA256 mySHA256 = SHA256Managed.Create();
            byte[] key = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(passPhrase));
            try
            {
                //// Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                //// so that the same Salt and IV values can be used when decrypting.  
                //var saltStringBytes = Generate256BitsOfRandomEntropy();
                //var ivStringBytes = Generate256BitsOfRandomEntropy();
                //var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                //using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                //{
                //    var keyBytes = password.GetBytes(Keysize / 8);
                //    using (var symmetricKey = new RijndaelManaged())
                //    {
                //        symmetricKey.BlockSize = 256;
                //        symmetricKey.Mode = CipherMode.CBC;
                //        symmetricKey.Padding = PaddingMode.PKCS7;
                //        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                //        {
                //            using (var memoryStream = new MemoryStream())
                //            {
                //                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                //                {
                //                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                //                    cryptoStream.FlushFinalBlock();
                //                    // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                //                    var cipherTextBytes = saltStringBytes;
                //                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                //                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                //                    memoryStream.Close();
                //                    cryptoStream.Close();
                //                    return Convert.ToBase64String(cipherTextBytes);
                //                }
                //            }
                //        }
                //    }
                //}

                // Instantiate a new Aes object to perform string symmetric encryption
                Aes encryptor = Aes.Create();

                encryptor.Mode = CipherMode.CBC;

                // Set key and IV
                byte[] aesKey = new byte[32];
                Array.Copy(key, 0, aesKey, 0, 32);
                encryptor.Key = aesKey;
                encryptor.IV = iv;

                // Instantiate a new MemoryStream object to contain the encrypted bytes
                MemoryStream memoryStream = new MemoryStream();

                // Instantiate a new encryptor from our Aes object
                ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();

                // Instantiate a new CryptoStream object to process the data and write it to the 
                // memory stream
                CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

                // Convert the plainText string into a byte array
                byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);

                // Encrypt the input plaintext string
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);

                // Complete the encryption process
                cryptoStream.FlushFinalBlock();

                // Convert the encrypted data from a MemoryStream to a byte array
                byte[] cipherBytes = memoryStream.ToArray();

                // Close both the MemoryStream and the CryptoStream
                memoryStream.Close();
                cryptoStream.Close();

                // Convert the encrypted byte array to a base64 encoded string
                string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

                // Return the encrypted data as a string
                return cipherText;
            }
            catch (Exception e)
            {
                return String.Empty;
            }
        }

        public static string Decrypt(this string cipherText, string passPhrase)
        {
            try
            {
                if (string.IsNullOrEmpty(cipherText))
                    return string.Empty;


                byte[] iv = new byte[16] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
                SHA256 mySHA256 = SHA256Managed.Create();
                byte[] key = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(passPhrase));
                //// Get the complete stream of bytes that represent:
                //// [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                //var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                //// Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                //var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                //// Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                //var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                //// Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                //var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                //using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                //{
                //    var keyBytes = password.GetBytes(Keysize / 8);
                //    using (var symmetricKey = new RijndaelManaged())
                //    {
                //        symmetricKey.BlockSize = 256;
                //        symmetricKey.Mode = CipherMode.CBC;
                //        symmetricKey.Padding = PaddingMode.PKCS7;
                //        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                //        {
                //            using (var memoryStream = new MemoryStream(cipherTextBytes))
                //            {
                //                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                //                {
                //                    var plainTextBytes = new byte[cipherTextBytes.Length];
                //                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                //                    memoryStream.Close();
                //                    cryptoStream.Close();
                //                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                //                }
                //            }
                //        }
                //    }
                //}
                // Instantiate a new Aes object to perform string symmetric encryption
                Aes encryptor = Aes.Create();

                encryptor.Mode = CipherMode.CBC;

                // Set key and IV
                byte[] aesKey = new byte[32];
                Array.Copy(key, 0, aesKey, 0, 32);
                encryptor.Key = aesKey;
                encryptor.IV = iv;

                // Instantiate a new MemoryStream object to contain the encrypted bytes
                MemoryStream memoryStream = new MemoryStream();

                // Instantiate a new encryptor from our Aes object
                ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();

                // Instantiate a new CryptoStream object to process the data and write it to the 
                // memory stream
                CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

                // Will contain decrypted plaintext
                string plainText = String.Empty;

                try
                {
                    // Convert the ciphertext string into a byte array
                    byte[] cipherBytes = Convert.FromBase64String(cipherText);

                    // Decrypt the input ciphertext string
                    cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);

                    // Complete the decryption process
                    cryptoStream.FlushFinalBlock();

                    // Convert the decrypted data from a MemoryStream to a byte array
                    byte[] plainBytes = memoryStream.ToArray();

                    // Convert the decrypted byte array to string
                    plainText = Encoding.ASCII.GetString(plainBytes, 0, plainBytes.Length);
                }
                finally
                {
                    // Close both the MemoryStream and the CryptoStream
                    memoryStream.Close();
                    cryptoStream.Close();
                }

                // Return the decrypted data as a string
                return plainText;
            }
            catch (Exception e)
            {
                return String.Empty;
            }
        }
        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }
        public static string ToBase64(this string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string FromBase64(this string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
