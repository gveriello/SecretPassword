using Entities;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace Business
{
    public static class BrowserPasswordsImporter
    {

        #region Read from Chrome
        public static string BrowserName { get { return "Chrome"; } }

        private const string LOGIN_DATA_PATH = "\\..\\Local\\Google\\Chrome\\User Data\\Default\\Login Data";
        public static IList<Credential> ImportFromChrome()
        {
            var result = new List<Credential>();

            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);// APPDATA
            var p = Path.GetFullPath(appdata + LOGIN_DATA_PATH);

            if (File.Exists(p))
            {
                using (var conn = new SQLiteConnection($"Data Source={p};Version=3;New=True;Compress=True;"))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT action_url, username_value, password_value FROM logins"; 
                        SQLiteDataReader reader = null;
                        try
                        {
                            reader = cmd.ExecuteReader();
                        }
                        catch(Exception ex)
                        {
                            if (ex.Message.ToString().ToLower().Contains("database is locked"))
                                throw new Exception("Google Chrome sta bloccando l' importazione delle utenze. Killare il processo tramite Task Manager e riprovare.");
                        }
                        finally
                        {
                            if (reader == null)
                                conn.Close();

                            if (reader != null)
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        string password = string.Empty;
                                        try
                                        {
                                            byte[] passwordLocked = ProtectedData.Unprotect((byte[])reader["password_value"], null, DataProtectionScope.CurrentUser);
                                            password = Encoding.UTF8.GetString(passwordLocked);
                                        }
                                        catch
                                        {

                                        }
                                        finally
                                        {
                                            Credential newCredential = new Credential();
                                            newCredential.Url = reader.GetString(0);
                                            newCredential.Email = reader.GetString(1);
                                            newCredential.Password = password;
                                            newCredential.Title = new Uri(newCredential.Url).Host;
                                            if (string.IsNullOrEmpty(newCredential.Title))
                                                newCredential.Title = newCredential.Url;
                                            result.Add(newCredential);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new FileNotFoundException("Chrome non contiene credenziali memorizzate.");
            }
            return result;
        }

        private static byte[] GetBytes(SQLiteDataReader reader, int columnIndex)
        {
            const int CHUNK_SIZE = 2 * 1024;
            byte[] buffer = new byte[CHUNK_SIZE];
            long bytesRead;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                while ((bytesRead = reader.GetBytes(columnIndex, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, (int)bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }
        #endregion
    }
}
