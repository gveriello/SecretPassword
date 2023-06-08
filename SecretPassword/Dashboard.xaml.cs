using Business;
using Entities;
using Microsoft.Win32;
using Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using System.Windows.Data;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace SecretPassword
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dashboard Model { get; set; }


        public MainWindow()
        {
            InitializeComponent();
            Helpers.RegisterCustomUrlProtocol();
            Helpers.CreateDatabasesIfNotExists();
            this.Model = new Dashboard();
            this.DataContext = this.Model;
            this.Loaded += this.MainWindow_Loaded;
        }

        public MainWindow(string[] args) 
            : this()
        {
        //    MessageBox.Show(args.ToString());
        }

        private void LoadTree()
        {
            if (this.Model.IsLocked)
                return;

            tviMyGroups.Items.Clear();
            this.Model.GroupsSource = Groups.LoadGroupsFromLocal();
            foreach (Group group in this.Model.GroupsSource)
                this.AddNewTreeItem(group);
        }

        private void AddNewTreeItem(Group group)
        {
            if (group == null)
                return;

            TreeViewItem item = new TreeViewItem();
            item.Name = $"Group{group.Name.Replace(" ", string.Empty)}{group.ID.ToString()}";
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            panel.Children.Add(new TextBlock() { Text = group.Name });
            item.Header = panel;
            item.Tag = group;
            this.tviMyGroups.Items.Add(item);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Load();
        }

        private void Load()
        {
            Helpers.CheckIfExistSalt();
            this.Model.IsLocked = !Helpers.ConvalidateSalt();
            if (this.CheckLockProgram())
                return;

            this.Height = this.MinHeight;
            this.Width = this.MinWidth;
            
            this.LoadTree();
            this.tvGroups.SelectedItemChanged += this.TvGroups_SelectedItemChanged;
        }

        private bool CheckLockProgram()
        {
            if (this.Model.IsLocked)
                MessageBox.Show("L'applicazione è in stato di lock; sblocca prima di proseguire.", "SecretPassword");

            return this.Model.IsLocked;
        }

        private void TvGroups_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this.CheckLockProgram())
            {
                e.Handled = true;
                return;
            }

            object tag = ((e.NewValue as TreeViewItem).Tag);
            this.Model.GroupSelectedObject = tag;
            this.Reset();
            ReloadCredentialsSource();
        }

        private void Reset()
        {
            if (this.CheckLockProgram())
                return;

            this.Model.ClearNewCredential();
            this.txtNewPassword.Password = string.Empty;
            this.Model.ClearNewGroup();
            this.Model.ClearModifyCredential();
            this.Model.ClearModifyGroup();
            this.Model.ClearShare();
            this.dgtcGroupName.Visibility = (this.Model.IsRootSelected) ? Visibility.Visible : Visibility.Collapsed;
            this.dgCredentials.Visibility = (this.Model.IsRootSelected || this.Model.IsGroupSelected) ? Visibility.Visible : Visibility.Collapsed;
            this.grdOopsEmpty.Visibility = this.dgCredentials.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowHidePassword(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            Credential row = (sender as Button).DataContext as Credential;
            
            row.ShowPassword = !row.ShowPassword && Helpers.ConvalidateSalt(isRequired: false);
            
            this.Model.ReloadCredentialsGrid();
        }

        private void CopyTextInClipBoard(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            string textToCopy = (sender as Button)?.Tag?.ToString();
            if (string.IsNullOrEmpty(textToCopy))
                return;

            Clipboard.SetText(textToCopy);
        }

        private void OpenURL(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            Credential row = (sender as Button).DataContext as Credential;
            if (string.IsNullOrEmpty(row.Url))
                return;

            
            string url = row.Url;
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
            
        }

        private void ReloadCredentialsSource()
        {
            if (this.Model.IsLocked)
            {
                this.Model.CredentialsSource = null;
                return;
            }

            this.Model.CredentialsSource = Credentials.LoadCredentialsLocalByGroupID(this.Model.CredentialGroupID.GetValueOrDefault());
        }

        private void BtnGroupAdd_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            this.Model.AddGroup = !this.Model.AddGroup;
            this.Model.AddCredential = false;
        }

        private void BtnCredentialAdd_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            this.Model.AddCredential = !this.Model.AddCredential;
        }

        private void BtnSaveGroup_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            try
            {
                if (this.Model.ModifyGroup)
                {
                    Groups.Modify(this.Model.ModifyGroupID, this.Model.NewGroupName, this.Model.NewGroupNotes);
                }
                else
                {
                    int newGroupID = Groups.Add(this.Model.NewGroupName, this.Model.NewGroupNotes);
                    this.AddNewTreeItem(Groups.GetById(newGroupID));
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                if (this.Model.AddGroup)
                this.Model.ClearNewGroup();
            }
            finally
            {
                this.Reset();
                this.LoadTree();
            }
        }

        private void BtnSaveCredential_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            try
            {
                if (this.Model.ModifyCredential)
                    if (this.Model.ModifyCredentialID == 0)
                    {
                        if (MessageBox.Show("SecretPassword", "Stai modificando la Master Password", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            Credentials.ModifyMasterPassword(this.Model.NewCredentialPassword);
                    }
                    else
                        Credentials.Modify(this.Model.ModifyCredentialID, this.Model.GroupSelected, this.Model.NewCredentialTitle, this.Model.NewCredentialUsername, this.Model.NewCredentialEmail, this.Model.NewCredentialPassword, this.Model.NewCredentialUrl, this.Model.NewCredentialNotes, this.Model.NewCredentialExpires);
                else
                    Credentials.Add(this.Model.GroupSelected, this.Model.NewCredentialTitle, this.Model.NewCredentialUsername, this.Model.NewCredentialEmail, this.Model.NewCredentialPassword, this.Model.NewCredentialUrl, this.Model.NewCredentialNotes, this.Model.NewCredentialExpires);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                if (this.Model.AddCredential)
                    this.Model.ClearNewCredential();
            }
            finally
            {
                this.Reset();
                ReloadCredentialsSource();
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this.Model.NewCredentialPassword = this.txtNewPassword.Password;
        }

        private void BtnCredentialRemove_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            try
            {
                var result = MessageBox.Show("Stai eliminado la password. Proseguire?", "SecretPassword", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                    Credentials.Delete(this.Model.CredentialSelected.ID);
            }
            finally
            {
                ReloadCredentialsSource();
            }
        }

        private void BtnGroupRemove_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            try
            {
                var result = MessageBox.Show("Stai eliminando un gruppo. Proseguire?", "SecretPassword", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    Credentials.DeleteAllFromGroup(this.Model.GroupSelected.ID);
                    Groups.Delete(this.Model.GroupSelected.ID);
                }
            }
            finally
            {
                this.LoadTree();
            }
        }

        private void BtnGroupModify_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            if (!this.Model.ModifyGroup)
            {
                this.Model.ModifyGroupID = this.Model.GroupSelected.ID;
                this.Model.NewGroupName = this.Model.GroupSelected.Name;
                this.Model.NewGroupNotes = this.Model.GroupSelected.Notes;
            }
            this.Model.ModifyGroup = !this.Model.ModifyGroup;
            
        }

        private void BtnCredentialModify_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            if (!this.Model.ModifyCredential)
            {
                this.Model.ModifyCredentialID = this.Model.CredentialSelected.ID;
                this.Model.NewCredentialTitle = this.Model.CredentialSelected.Title;
                this.Model.NewCredentialUsername = this.Model.CredentialSelected.Username;
                this.Model.NewCredentialEmail = this.Model.CredentialSelected.Email;
                this.txtNewPassword.Password = this.Model.CredentialSelected.Password;
                this.Model.NewCredentialUrl = this.Model.CredentialSelected.Url;
                this.Model.NewCredentialExpires = this.Model.CredentialSelected.Expires;
                this.Model.NewCredentialNotes = this.Model.CredentialSelected.Notes;
            }
            this.Model.ModifyCredential = !this.Model.ModifyCredential;
        }

        private void ShowHideStreamToShare(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            if (!this.Model.ShareCredential)
            {
                string hash = Credentials.Export(this.Model.CredentialSelected.ID);
                this.Model.ShareCredentialString = hash;
            }
            this.Model.ShareCredential = !this.Model.ShareCredential && Helpers.ConvalidateSalt(isRequired: false);
        }

        private void BtnCredentialImport_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            try
            {
                
                Credentials.Import(this.Model.GroupSelected);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.ReloadCredentialsSource();
                
            }
        }

        private void BtnCredentialBackup_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            Credentials.CreateBackup();
            MessageBox.Show($"Il backup è stato creato con successo ed è stato salvato in questo percorso: {Path.Combine(Directory.GetCurrentDirectory(), "backup")}.", "SecretPassword");
        }

        private void BtnCredentialImportBackup_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            
            int errors = Credentials.ImportBackup();
            string msgError = errors > 0 ? $" {errors} record non sono stati importati per errori." : string.Empty;
            MessageBox.Show($"Import completato.{msgError}");
            this.ReloadCredentialsSource();
            
        }

        private void BtnCredentialImportChrome_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            try
            {
                int errors = Credentials.ImportChrome(this.Model.GroupSelected);
                string msgError = errors > 0 ? $" {errors} record non sono stati importati per errori." : string.Empty;
                MessageBox.Show($"Import completato.{msgError}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.ReloadCredentialsSource();
            }
        }

        private void DgCredentials_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1 ).ToString();
        }

        private void BtnLock_Click(object sender, RoutedEventArgs e)
        {
            this.Model.IsLocked = true;
            this.LoadTree();
            this.ReloadCredentialsSource();
            Helpers.Lock();
        }

        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            
            this.Load();
            this.ReloadCredentialsSource();
        }

        private void ShowHideQrCode(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            
            this.Reset();
            if (!this.Model.BuildQRCode)
            {
                var qrCode = Credentials.BuildQRCode(this.Model.CredentialSelected);
                this.Model.QrCodeToShare = qrCode;
            }
            this.Model.BuildQRCode = !this.Model.BuildQRCode && Helpers.ConvalidateSalt(isRequired: false);
            
        }

        private void CloseOperationPanel(object sender, RoutedEventArgs e)
        {
            this.Reset();
        }
    }


   
}
