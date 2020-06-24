using Entities;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Business
{
    public static class Helpers
    {
        static string AppDataPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecretPassword"); } }
        static string DatabaseGroupsPath { get { return Path.Combine(AppDataPath, "dbag.sp"); } }
        static string DatabaseCredentialsPath { get { return Path.Combine(AppDataPath, "dbac.sp"); } }
        static string UsersSalt { get; set; }


        public static string GetCredentialCripted(Credential credential)
        {
            if (credential != null)
            {
                credential.ShowPassword = true;
                string salt = Path.GetRandomFileName().Replace(".", "");

                string credentialSerialized = JsonConvert.SerializeObject(credential);
                string credentialCrypted = credentialSerialized.Encrypt(salt);
                return $"Stream|{credentialCrypted}|Salt|{salt}";
            }
            throw new Exception("Impossibile creare lo stream di condivisione.");
        }

        public static void CheckIfExistSalt()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\SP", true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\SP");
                key.SetValue("SPSalt", AskSalt().Encrypt(Path.GetRandomFileName().Replace(".", "")));
            }
            if (key != null)
            {
                if (string.IsNullOrEmpty(key.GetValue("SPSalt")?.ToString()))
                    key.SetValue("SPSalt", AskSalt().Encrypt(Path.GetRandomFileName().Replace(".", "")));
            }
            UsersSalt = key.GetValue("SPSalt").ToString();
            key.Close();
        }

        public static string AskSalt()
        {
            bool isAsked = false;
            while(true)
            {
                string input = Interaction.InputBox((isAsked ? "La password è obbligatoria." : string.Empty), "Inserisci la password con cui cripteremo/decripteremo i tuoi dati.");
                if (!string.IsNullOrEmpty(input))
                    return input;
                isAsked = true;
            }
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
    }
    public static class Crypto
    {
        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        public static string Encrypt(this string plainText, string passPhrase)
        {
            try
            {
                // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                // so that the same Salt and IV values can be used when decrypting.  
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
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

                // Get the complete stream of bytes that represent:
                // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
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
    }
}
