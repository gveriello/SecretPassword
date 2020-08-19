using Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Models
{
    public class Dashboard : INotifyPropertyChanged
    {
        IList<Credential> _credentials;
        Credential _credentialSelected;
        object _groupSelectedObject;
        bool _AddGroup = false;
        bool _AddCredential = false;
        string _NewCredentialTitle;
        string _NewGroupName;
        string _NewGroupNotes;
        string _NewCredentialUsername;
        string _NewCredentialEmail;
        string _NewCredentialPassword;
        string _NewCredentialUrl;
        DateTime? _NewCredentialExpires;
        string _NewCredentialNotes;
        string _ShareCredentialString;
        bool _ModifyGroup;
        bool _ModifyCredential;
        bool _ShareCredential;
        bool _isLocked;
        bool _buildQrCode;
        byte[] _qrCodeToShare;

        public object GroupSelectedObject
        {
            get { return this._groupSelectedObject; }
            set
            {
                this._groupSelectedObject = value;
                PropertyIsChanged();
                PropertyIsChanged(nameof(this.GroupSelected));
                PropertyIsChanged(nameof(this.IsGroupSelected));
            }
        }

        public bool IsRootSelected {
            get
            {
                if (this.GroupSelected != null || this.GroupSelectedObject == null)
                    return false;

                int tagGroup = 0;
                int.TryParse(this.GroupSelectedObject?.ToString(), out tagGroup);
                return tagGroup == 0;
            }
        }
        public Group GroupSelected
        {
            get { return this.GroupSelectedObject != null ? this.GroupSelectedObject as Group : null; }
        }

        public Credential CredentialSelected
        {
            get { return this._credentialSelected; }
            set
            {
                this._credentialSelected = value;
                PropertyIsChanged();
                PropertyIsChanged(nameof(this.IsCredentialSelected));
            }
        }
        public IList<Group> GroupsSource { get; set; }



        public bool BuildQRCode
        {
            get { return this._buildQrCode; }
            set
            {
                this._buildQrCode = value;
                PropertyIsChanged();
                PropertyIsChanged(nameof(this.OperationInProgress));
            }
        }

        public bool IsLocked
        {
            get { return this._isLocked; }
            set
            {
                this._isLocked = value;
                PropertyIsChanged();
            }
        }

        public IList<Credential> CredentialsSource
        {
            get { return this._credentials; }
            set
            {
                this._credentials = value;
                ReloadCredentialsGrid();
            }
        }

        public void ReloadCredentialsGrid()
        {
            PropertyIsChanged(nameof(this.Credentials));
        }

        public void ClearModifyCredential()
        {
            this.ModifyCredential = false;
            this.ModifyCredentialID = 0;
            this.ClearNewCredential();
        }

        public void ClearModifyGroup()
        {
            this.ModifyGroup = false;
            this.ModifyGroupID = -1;
        }

        public void ClearShare()
        {
            this.BuildQRCode = false;
            this.QrCodeToShare = null;
            this.ShareCredential = false;
            this.ShareCredentialString = string.Empty;
        }

        /* VISUAL */
        public bool IsGroupSelected { get { return this.GroupSelected != null;  } }
        public bool IsCredentialSelected { get { return this.CredentialSelected != null; } }
        public ObservableCollection<Group> Groups
        {
            get
            {
                var groups = new ObservableCollection<Group>();
                return groups;
            }
        }

        public ObservableCollection<Credential> Credentials
        {
            get
            {
                if (this.CredentialsSource == null)
                    return new ObservableCollection<Credential>();

                var credentials = new ObservableCollection<Credential>();

                foreach(Credential credential in this.CredentialsSource)
                    credentials.Add(credential);

                return credentials;
            }
        }
        public int? CredentialGroupID
        {
            get
            {
                if (this.GroupSelectedObject != null)
                {
                    if (this.GroupSelected != null)
                        return this.GroupSelected.ID;
                    return 0;
                }
                return -1;
            }
        }

        public bool AddModifyGroups
        {
            get { return this.AddGroup || this.ModifyGroup; }
        }
        public bool AddGroup
        {
            get { return this._AddGroup; }
            set
            {
                this._AddGroup = value;
                PropertyIsChanged();
                PropertyIsChanged(nameof(this.AddModifyGroups));
                PropertyIsChanged(nameof(this.OperationInProgress));
            }
        }
        public bool ModifyGroup
        {
            get { return this._ModifyGroup; }
            set
            {
                this._ModifyGroup = value;
                PropertyIsChanged();
                PropertyIsChanged(nameof(this.AddModifyGroups));
                PropertyIsChanged(nameof(this.OperationInProgress));
            }
        }
        public int ModifyGroupID { get; set; }

        public bool AddModifyCredential
        {
            get { return this.AddCredential || this.ModifyCredential; }
        }

        public bool OperationInProgress
        {
            get { return this.AddModifyGroups || this.AddModifyCredential || this.ShareCredential || this.BuildQRCode; }
        }

        public bool AddCredential
        {
            get { return this._AddCredential; }
            set
            {
                this._AddCredential = value;
                PropertyIsChanged();
                PropertyIsChanged(nameof(this.AddModifyCredential));
                PropertyIsChanged(nameof(this.OperationInProgress));
            }
        }
        public bool ModifyCredential
        {
            get { return this._ModifyCredential; }
            set
            {
                this._ModifyCredential = value;
                PropertyIsChanged();
                PropertyIsChanged(nameof(this.AddModifyCredential));
                PropertyIsChanged(nameof(this.OperationInProgress));
            }
        }
        public int ModifyCredentialID { get; set; }
        public bool ShareCredential
        {
            get { return this._ShareCredential; }
            set
            {
                this._ShareCredential = value;
                PropertyIsChanged();
                PropertyIsChanged(nameof(this.OperationInProgress));
            }
        }
        public string ShareCredentialString
        {
            get
            {
                return this._ShareCredentialString;
            }
            set
            {
                this._ShareCredentialString = value;
                PropertyIsChanged();
            }
        }





        //New group
        public string NewGroupName
        {
            get
            {
                return this._NewGroupName;
            }
            set
            {
                this._NewGroupName = value;
                PropertyIsChanged();
            }
        }
        public string NewGroupNotes
        {
            get
            {
                return this._NewGroupNotes;
            }
            set
            {
                this._NewGroupNotes = value;
                PropertyIsChanged();
            }
        }
        public void ClearNewGroup()
        {
            this.AddGroup = false;
            this.NewGroupName = string.Empty;
            this.NewGroupNotes = string.Empty;
        }
        //New credential
        public string NewCredentialTitle
        {
            get
            {
                return this._NewCredentialTitle;
            }
            set
            {
                this._NewCredentialTitle = value;
                PropertyIsChanged();
            }
        }
        public string NewCredentialUsername
        {
            get
            {
                return this._NewCredentialUsername;
            }
            set
            {
                this._NewCredentialUsername = value;
                PropertyIsChanged();
            }
        }
        public string NewCredentialEmail
        {
            get
            {
                return this._NewCredentialEmail;
            }
            set
            {
                this._NewCredentialEmail = value;
                PropertyIsChanged();
            }
        }
        public string NewCredentialPassword
        {
            get
            {
                return this._NewCredentialPassword;
            }
            set
            {
                this._NewCredentialPassword = value;
                PropertyIsChanged();
            }
        }
        public string NewCredentialUrl
        {
            get
            {
                return this._NewCredentialUrl;
            }
            set
            {
                this._NewCredentialUrl = value;
                PropertyIsChanged();
            }
        }
        public DateTime? NewCredentialExpires
        {
            get
            {
                return this._NewCredentialExpires;
            }
            set
            {
                this._NewCredentialExpires = value;
                PropertyIsChanged();
            }
        }
        public string NewCredentialNotes
        {
            get
            {
                return this._NewCredentialNotes;
            }
            set
            {
                this._NewCredentialNotes = value;
                PropertyIsChanged();
            }
        }

        public byte[] QrCodeToShare
        {
            get
            {
                return this._qrCodeToShare;
            }
            set
            {
                this._qrCodeToShare = value;
                PropertyIsChanged();
            }
        }

        public void ClearNewCredential()
        {
            this.AddCredential = false;
            this.NewCredentialTitle = string.Empty;
            this.NewCredentialUsername = string.Empty;
            this.NewCredentialEmail = string.Empty;
            this.NewCredentialPassword = string.Empty;
            this.NewCredentialUrl = string.Empty;
            this.NewCredentialNotes = string.Empty;
            this.NewCredentialExpires = DateTime.Today;
        }
        /* INotifyPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void PropertyIsChanged([CallerMemberName] string caller = "")
        {

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }
    }
}
