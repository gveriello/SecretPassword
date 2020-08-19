using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretPassword
{
    public static class LanguageResources
    {
        public const string IsActuallyLocked = "SecretPassword è attualmente lockato. Clicca su 'Gestione', quindi 'Sblocca' per unlockare le informazioni.";
        public const string Answer_ConfirmOperation = "Sicuro di voler procedere?";

        public const string Answer_ConfirmMasterPasswordModify = "Modificando la Master Password, potresti perdere alcune informazioni. Sei sicuro di voler procedere?";
        public const string Answer_ConfirmDeleteGroup = "Sicuro di voler procedere? Verranno eliminate anche le credenziali associate";
        public const string SuccesfullyBackup = "Backup creato in {0}. Affinchè possa essere reimportato correttamente, dovrai inserire la tua attuale Master Password quando richiesto.";
        public const string DeleteCredentials = "Elimina credenziali";
        public const string DeleteGroup = "Elimina gruppo";
    }
}
