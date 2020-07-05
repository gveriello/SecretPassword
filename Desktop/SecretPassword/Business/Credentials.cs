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
            credentials?.Clear();
            LoadMasterPassword();

            string credentialFromFile = Helpers.ReadCredentials();
            if (!string.IsNullOrEmpty(credentialFromFile))
                credentials = credentials.Concat(JsonConvert.DeserializeObject<IList<Credential>>(credentialFromFile).ToList()).ToList();

            foreach (Credential credential in credentials)
                credential.ShowPassword = false;
        }

        private static void LoadMasterPassword()
        {
            CheckCredentialsLoaded();

            if (credentials.Any(c => c.ID == 0))
                return;

            Credential masterPassword = new Credential();
            masterPassword.ID = 0;
            masterPassword.Title = "Master Password";
            masterPassword.Username = string.Empty;
            masterPassword.Email = string.Empty;
            masterPassword.Password = Helpers.GetOrAskSalt();
            masterPassword.ShowPassword = false;
            masterPassword.Url = string.Empty;
            masterPassword.Expires = null;

            credentials.Add(masterPassword);
            credentials = credentials.OrderBy(c => c.ID).ToList();
        }

        public static IList<Credential> LoadCredentialsLocalByGroupID(int groupID)
        {
            LoadAll();

            IList<Credential> credentialsTemp = null;

            if (groupID < 0)
                credentialsTemp = new List<Credential>();

            if (groupID == 0)
                credentialsTemp = new List<Credential>(credentials);

            if (groupID > 0)
                credentialsTemp = credentials.Where(c => c.GroupID == groupID).ToList();

            foreach (Credential credential in credentialsTemp)
                credential.ShowPassword = false;

            return credentialsTemp;
        }

        private static void CheckCredentialsLoaded()
        {
            if (credentials == null)
                credentials = new List<Credential>();
        }

        public static int Add(Group group, string newCredentialTitle, string newCredentialUsername, string newCredentialEmail, string newCredentialPassword, string newCredentialUrl, string newCredentialNotes, DateTime? newCredentialExpires)
        {
            CheckCredentialsLoaded();

            if (string.IsNullOrEmpty(newCredentialTitle))
                throw new Exception("Il titolo non può essere vuoto.");

            if (string.IsNullOrEmpty(newCredentialEmail))
                throw new Exception("L' e-mail non può essere vuota.");

            if (string.IsNullOrEmpty(newCredentialPassword))
                throw new Exception("La password non può essere vuota.");

            int existingSameCredentials = credentials.Count(g => g.Title.ToLower() == newCredentialTitle && g.GroupID.GetValueOrDefault() == group?.ID);
            if (existingSameCredentials > 0)
                newCredentialTitle = newCredentialTitle + $"({existingSameCredentials + 1})";

            int proxID = 1;
            if (credentials.Count > 0)
                proxID = credentials.Max(g => g.ID) + 1;

            Credential newCredential = new Credential();
            newCredential.ID = proxID;
            newCredential.GroupOwner = group;
            newCredential.Title = newCredentialTitle;
            newCredential.Username = newCredentialUsername;
            newCredential.Email = newCredentialEmail;
            newCredential.Password = newCredentialPassword;
            newCredential.Notes = newCredentialNotes;
            newCredential.Url = newCredentialUrl;
            newCredential.Expires = newCredentialExpires;

            credentials.Add(newCredential);
            credentials = credentials.OrderBy(c => c.ID).ToList();

            Save();

            return proxID;
        }

        public static void ModifyMasterPassword(string newSalt)
        {
            if (string.IsNullOrEmpty(newSalt))
                return;

            credentials.ToList().ForEach(c => c.ShowPassword = false);
            Helpers.ChangeSalt(newSalt);
            Helpers.SaveCredentials(JsonConvert.SerializeObject(credentials.Where(c => c.ID > 0)));
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
                credentialGroup.ToList().ForEach(c => {
                    c.ShowPassword = false;
                    credentials.Remove(c);
                });
            Save();
        }

        public static void Save()
        {
            CheckCredentialsLoaded();

            credentials.ToList().ForEach(c => c.ShowPassword = false);
            Helpers.SaveCredentials(JsonConvert.SerializeObject(credentials.Where(c => c.ID != 0)));
        }

        public static string Export(int credentialID)
        {
            CheckCredentialsLoaded();

            Credential credential = credentials.FirstOrDefault(c => c.ID == credentialID);
            if (credential != null)
                return Helpers.GetCredentialCripted(credential);

            throw new Exception("Impossibile trovare l' elemento.");
        }

        public static void Modify(int modifyCredentialID, Group group, string newCredentialTitle, string newCredentialUsername, string newCredentialEmail, string newCredentialPassword, string newCredentialUrl, string newCredentialNotes, DateTime? newCredentialExpires)
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

                int existingSameCredentials = credentials.Count(c => c.Title.ToLower() == newCredentialTitle && c.GroupOwner.ID == group.ID && c.ID != credential.ID);
                if (existingSameCredentials > 0)
                    credential.Title = credential.Title + $"({existingSameCredentials + 1})";

                int proxID = 1;
                if (credentials.Count > 0)
                    proxID = credentials.Max(g => g.ID) + 1;

                credential.GroupOwner = group;
                credential.Title = newCredentialTitle;
                credential.Username = newCredentialUsername;
                credential.Email = newCredentialEmail;
                credential.Password = newCredentialPassword;
                credential.Notes = newCredentialNotes;
                credential.Url = newCredentialUrl;
                credential.Expires = newCredentialExpires;
                credentials.Remove(credential);
                credentials.Add(credential);
                credentials = credentials.OrderBy(c => c.ID).ToList();
            }
            Save();
        }

        public static void Import(Group groupOwner)
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

            int existingSameCredentials = credentials.Count(c => c.Title.ToLower() == credentialImported.Title.ToLower() && c.GroupOwner == groupOwner);
            if (existingSameCredentials > 0)
                credentialImported.Title = credentialImported.Title + $"({existingSameCredentials + 1})";

            credentialImported.ID = proxID;
            credentialImported.GroupOwner = groupOwner;

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

        public static int ImportChrome(Group group)
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

                    int existingSameCredentials = credentials.Count(c => c.Title.ToLower() == tempCredential.Title.ToLower() && c.GroupOwner == group);
                    if (existingSameCredentials > 0)
                        tempCredential.Title = tempCredential.Title + $"({existingSameCredentials + 1})";

                    tempCredential.GroupOwner = group;

                    credentials.Add(tempCredential);
                    
                }
                Save();
                return msgError;
            }
            return 0;
        }
    }
}
