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
        string _NewCredentialExpires;
        string _NewCredentialNotes;
        string _ShareCredentialString;
        bool _ModifyGroup;
        bool _ModifyCredential;
        bool _ShareCredential;

        public object GroupSelectedObject
        {
            get { return this._groupSelectedObject; }
            set
            {
                this._groupSelectedObject = value;
                RaiseProperChanged();
                RaiseProperChanged(nameof(this.GroupSelected));
                RaiseProperChanged(nameof(this.IsGroupSelected));
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
                RaiseProperChanged();
                RaiseProperChanged(nameof(this.IsCredentialSelected));
            }
        }
        public IList<Group> GroupsSource { get; set; }

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
            RaiseProperChanged(nameof(this.Credentials));
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
                RaiseProperChanged();
                RaiseProperChanged(nameof(this.AddModifyGroups));
            }
        }
        public bool ModifyGroup
        {
            get { return this._ModifyGroup; }
            set
            {
                this._ModifyGroup = value;
                RaiseProperChanged();
                RaiseProperChanged(nameof(this.AddModifyGroups));
            }
        }
        public int ModifyGroupID { get; set; }

        public bool AddModifyCredential
        {
            get { return this.AddCredential || this.ModifyCredential; }
        }
        public bool AddCredential
        {
            get { return this._AddCredential; }
            set
            {
                this._AddCredential = value;
                RaiseProperChanged();
                RaiseProperChanged(nameof(this.AddModifyCredential));
            }
        }
        public bool ModifyCredential
        {
            get { return this._ModifyCredential; }
            set
            {
                this._ModifyCredential = value;
                RaiseProperChanged();
                RaiseProperChanged(nameof(this.AddModifyCredential));
            }
        }
        public int ModifyCredentialID { get; set; }
        public bool ShareCredential
        {
            get { return this._ShareCredential; }
            set
            {
                this._ShareCredential = value;
                RaiseProperChanged();
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
                RaiseProperChanged();
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
                RaiseProperChanged();
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
                RaiseProperChanged();
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
                RaiseProperChanged();
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
                RaiseProperChanged();
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
                RaiseProperChanged();
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
                RaiseProperChanged();
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
                RaiseProperChanged();
            }
        }
        public string NewCredentialExpires
        {
            get
            {
                return this._NewCredentialExpires;
            }
            set
            {
                this._NewCredentialExpires = value;
                RaiseProperChanged();
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
                RaiseProperChanged();
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
            this.NewCredentialExpires = string.Empty;
        }

        /* INotifyPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaiseProperChanged([CallerMemberName] string caller = "")
        {

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }
    }
}
