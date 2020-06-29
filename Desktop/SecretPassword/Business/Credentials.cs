using Entities;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business
{
    public static class Credentials
    {
        static string DatabaseCredentialsPath { get { return Path.Combine(Helpers.AppDataPath, "dbac.sp"); } }
        private static IList<Credential> credentials { get; set; }
        public static void LoadAll()
        {
            string credentialFromFile = Helpers.ReadCredentials();
            if (!string.IsNullOrEmpty(credentialFromFile))
                credentials = JsonConvert.DeserializeObject<IList<Credential>>(credentialFromFile).ToList();

            CheckCredentialsLoaded();

            foreach (Credential credential in credentials)
                credential.ShowPassword = false;
        }

        public static IList<Credential> LoadCredentialsLocalByGroupID(int groupID)
        {
            LoadAll();
            IList<Credential> credentialsTemp = credentials.Where(c => c.GroupID == groupID).ToList();
            foreach (Credential credential in credentialsTemp)
                credential.ShowPassword = false;

            return credentialsTemp;
        }

        private static void CheckCredentialsLoaded()
        {
            if (credentials == null)
                credentials = new List<Credential>();
        }

        public static int Add(int idGroup, string newCredentialTitle, string newCredentialUsername, string newCredentialEmail, string newCredentialPassword, string newCredentialUrl, string newCredentialNotes, string newCredentialExpires)
        {
            if (idGroup == -1)
                idGroup = 0;

            CheckCredentialsLoaded();

            if (string.IsNullOrEmpty(newCredentialTitle))
                throw new Exception("Il titolo non può essere vuoto.");

            if (string.IsNullOrEmpty(newCredentialEmail))
                throw new Exception("L' e-mail non può essere vuota.");

            if (string.IsNullOrEmpty(newCredentialPassword))
                throw new Exception("La password non può essere vuota.");

            int existingSameCredentials = credentials.Count(g => g.Title.ToLower() == newCredentialTitle && g.GroupID == idGroup);
            if (existingSameCredentials > 0)
                newCredentialTitle = newCredentialTitle + $"({existingSameCredentials + 1})";

            int proxID = 1;
            if (credentials.Count > 0)
                proxID = credentials.Max(g => g.ID) + 1;

            Credential newCredential = new Credential();
            newCredential.ID = proxID;
            newCredential.GroupID = idGroup;
            newCredential.Title = newCredentialTitle;
            newCredential.Username = newCredentialUsername;
            newCredential.Email = newCredentialEmail;
            newCredential.Password = newCredentialPassword;
            newCredential.Notes = newCredentialNotes;
            newCredential.Url = newCredentialUrl;

            if (!string.IsNullOrEmpty(newCredentialExpires))
            {
                DateTime expires = new DateTime();
                DateTime.TryParse(newCredentialExpires, out expires);
                newCredential.Expires = expires;
            }

            credentials.Add(newCredential);
            credentials = credentials.OrderBy(c => c.Title).ToList();

            Save();

            return proxID;
        }

        public static void Delete(int idCredential)
        {
            CheckCredentialsLoaded();

            credentials.Remove(credentials.FirstOrDefault(c => c.ID == idCredential));
            Save();
        }

        public static void DeleteAllFromGroup(int groupID)
        {
            CheckCredentialsLoaded();

            IList<Credential> credentialGroup = credentials.Where(c => c.GroupID == groupID).ToList();
            if (credentialGroup != null)
                foreach (Credential credential in credentialGroup)
                    credentials.Remove(credential);
            Save();
        }

        public static void Save()
        {
            CheckCredentialsLoaded();

            foreach (Credential credential in credentials) credential.ShowPassword = false;
            Helpers.SaveCredentials(JsonConvert.SerializeObject(credentials));
        }

        public static string Export(int credentialID)
        {
            CheckCredentialsLoaded();

            Credential credential = credentials.FirstOrDefault(c => c.ID == credentialID);
            if (credential != null)
                return Helpers.GetCredentialCripted(credential);

            throw new Exception("Impossibile trovare l' elemento.");
        }

        public static void Modify(int modifyCredentialID, int idGroup, string newCredentialTitle, string newCredentialUsername, string newCredentialEmail, string newCredentialPassword, string newCredentialUrl, string newCredentialNotes, string newCredentialExpires)
        {
            CheckCredentialsLoaded();

            Credential credential = credentials.FirstOrDefault(c => c.ID == modifyCredentialID);
            if (credential != null)
            {
                if (string.IsNullOrEmpty(newCredentialTitle))
                    throw new Exception("Il titolo non può essere vuoto.");

                if (string.IsNullOrEmpty(newCredentialEmail))
                    throw new Exception("L' e-mail non può essere vuota.");

                if (string.IsNullOrEmpty(newCredentialPassword))
                    throw new Exception("La password non può essere vuota.");

                int existingSameCredentials = credentials.Count(c => c.Title.ToLower() == newCredentialTitle && c.GroupID == idGroup && c.ID != credential.ID);
                if (existingSameCredentials > 0)
                    credential.Title = credential.Title + $"({existingSameCredentials + 1})";

                int proxID = 1;
                if (credentials.Count > 0)
                    proxID = credentials.Max(g => g.ID) + 1;

                credential.GroupID = idGroup;
                credential.Title = newCredentialTitle;
                credential.Username = newCredentialUsername;
                credential.Email = newCredentialEmail;
                credential.Password = newCredentialPassword;
                credential.Notes = newCredentialNotes;
                credential.Url = newCredentialUrl;

                if (!string.IsNullOrEmpty(newCredentialExpires))
                {
                    DateTime expires = new DateTime();
                    DateTime.TryParse(newCredentialExpires, out expires);
                    credential.Expires = expires;
                }
                credentials.Remove(credential);
                credentials.Add(credential);
                credentials = credentials.OrderBy(c => c.Title).ToList();
            }
            Save();
        }

        public static void Import(int? groupOwner)
        {
            CheckCredentialsLoaded();

            string stream = Helpers.AskStream();

            if (string.IsNullOrEmpty(stream))
                return;

            if (string.IsNullOrEmpty(stream))
                throw new InvalidOperationException("Stream non valido.");

            StreamToShare sharedStream = JsonConvert.DeserializeObject<StreamToShare>(stream.FromBase64());

            if (sharedStream == null)
                throw new InvalidOperationException("Stream non valido.");

            string streamCredential = sharedStream.Stream;
            string salt = sharedStream.Salt;

            Credential credentialImported = JsonConvert.DeserializeObject<Credential>(streamCredential.Decrypt(salt));
            if (credentialImported == null)
                throw new Exception("Impossibile importare le credenziali");

            int proxID = 1;
            if (credentials.Count > 0)
                proxID = credentials.Max(g => g.ID) + 1;

            if (string.IsNullOrEmpty(credentialImported.Title))
                throw new Exception("Il titolo non può essere vuoto.");

            if (string.IsNullOrEmpty(credentialImported.Email))
                throw new Exception("L' e-mail non può essere vuota.");

            if (string.IsNullOrEmpty(credentialImported.Password))
                throw new Exception("La password non può essere vuota.");

            int existingSameCredentials = credentials.Count(c => c.Title.ToLower() == credentialImported.Title.ToLower() && c.GroupID == groupOwner.GetValueOrDefault());
            if (existingSameCredentials > 0)
                credentialImported.Title = credentialImported.Title + $"({existingSameCredentials + 1})";

            credentialImported.ID = proxID;
            credentialImported.GroupID = groupOwner.GetValueOrDefault();

            credentials.Add(credentialImported);
            Save();
        }

        public static void CreateBackup()
        {
            LoadAll();
            Save();

            if (!Directory.Exists("backup"))
                Directory.CreateDirectory("backup");

            File.Copy(DatabaseCredentialsPath, $"backup/Backup{DateTime.Today.ToString("ddMMyyyy")}", true);
        }

        public static int ImportBackup()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                string stream = Helpers.ReadBackup(openFileDialog.FileName);
                if (!string.IsNullOrEmpty(stream))
                {
                    IList<Credential> tempCredentials = JsonConvert.DeserializeObject<IList<Credential>>(stream).ToList();
                    int msgError = 0;
                    if (tempCredentials?.Count > 0)
                    {
                        foreach(Credential tempCredential in tempCredentials)
                        {
                            bool isError = false;
                            int proxID = 1;
                            if (credentials.Count > 0)
                                proxID = credentials.Max(g => g.ID) + 1;

                            if (string.IsNullOrEmpty(tempCredential.Title))
                                if (!isError) isError = true;

                            if (string.IsNullOrEmpty(tempCredential.Email))
                                if (!isError) isError = true;

                            if (string.IsNullOrEmpty(tempCredential.Password))
                                if (!isError) isError = true;

                            if (isError)
                            {
                                msgError++;
                                continue;
                            }

                            int existingSameCredentials = credentials.Count(c => c.Title.ToLower() == tempCredential.Title.ToLower() && c.GroupID == tempCredential.GroupID.GetValueOrDefault());
                            if (existingSameCredentials > 0)
                                tempCredential.Title = tempCredential.Title + $"({existingSameCredentials + 1})";

                            credentials.Add(tempCredential);
                        }
                        Save();
                    }
                    return msgError;
                }
            }
            return 0;
        }

        public static int ImportChrome(int? groupID)
        {
            LoadAll();
            IList<Credential> tempCredentials = BrowserPasswordsImporter.ImportFromChrome();
            int msgError = 0;
            if (tempCredentials?.Count > 0)
            {
                foreach (Credential tempCredential in tempCredentials)
                {
                    bool isError = false;
                    int proxID = 1;
                    if (credentials.Count > 0)
                        proxID = credentials.Max(g => g.ID) + 1;

                    if (string.IsNullOrEmpty(tempCredential.Email))
                        if (!isError) isError = true;

                    if (string.IsNullOrEmpty(tempCredential.Password))
                        if (!isError) isError = true;

                    if (isError)
                    {
                        msgError++;
                        continue;
                    }

                    int existingSameCredentials = credentials.Count(c => c.Title.ToLower() == tempCredential.Title.ToLower() && c.GroupID == groupID.GetValueOrDefault());
                    if (existingSameCredentials > 0)
                        tempCredential.Title = tempCredential.Title + $"({existingSameCredentials + 1})";

                    tempCredential.GroupID = groupID.GetValueOrDefault();

                    credentials.Add(tempCredential);
                    
                }
                Save();
                return msgError;
            }
            return 0;
        }
    }
}
