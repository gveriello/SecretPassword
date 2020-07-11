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
            Helpers.CreateDatabasesIfNotExists();
            this.Model = new Dashboard();
            this.DataContext = this.Model;
            this.Loaded += this.MainWindow_Loaded;
        }

        private void LoadTree()
        {
            if (!this.Model.IsLocked)
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
            if (!this.CheckLockProgram())
                return;

            this.Height = this.MinHeight;
            this.Width = this.MinWidth;
            this.Topmost = true;
            this.LoadTree();
            this.tvGroups.SelectedItemChanged += this.TvGroups_SelectedItemChanged;
        }

        private bool CheckLockProgram()
        {
            if (this.Model.IsLocked)
                MessageBox.Show("SecretPassword è attualmente lockato. Clicca su 'Gestione', quindi 'Sblocca' per unlockare le informazioni.");

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
        }

        private void ShowHidePassword(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            Credential row = (sender as Button).DataContext as Credential;
            this.Topmost = false;
            row.ShowPassword = !row.ShowPassword && Helpers.ConvalidateSalt(isRequired: false);
            this.Topmost = true;
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

            this.Topmost = false;
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
            this.Topmost = true;
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
                        if (MessageBox.Show("Modificando la Master Password, potresti perdere alcune informazioni. Sei sicuro di voler procedere?", "Stai modificando la Master Password", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
                var result = MessageBox.Show("Sicuro di voler procedere?", "Elimina credenziali", MessageBoxButton.YesNo);
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
                var result = MessageBox.Show("Sicuro di voler procedere? Verranno eliminate anche le credenziali associate", "Elimina gruppo", MessageBoxButton.YesNo);
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

        private void BtnCredentialShare_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            if (!this.Model.ShareCredential)
            {
                string hash = Credentials.Export(this.Model.CredentialSelected.ID);
                this.Model.ShareCredentialString = hash;
            }
            this.Model.ShareCredential = !this.Model.ShareCredential;
        }

        private void BtnCredentialImport_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            this.Reset();
            try
            {
                this.Topmost = false;
                Credentials.Import(this.Model.GroupSelected);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.ReloadCredentialsSource();
                this.Topmost = true;
            }
        }

        private void BtnCredentialBackup_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckLockProgram())
                return;

            Credentials.CreateBackup();
            MessageBox.Show($"Backup creato in {Path.Combine(Directory.GetCurrentDirectory(), "backup")}.{Environment.NewLine}Affinchè possa essere reimportato correttamente, è necessario che la Master Password non cambi.");
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
            this.Topmost = false;
            this.Load();
            this.ReloadCredentialsSource();
        }
    }


   
}
