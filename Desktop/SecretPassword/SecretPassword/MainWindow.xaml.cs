using Business;
using Entities;
using Microsoft.Win32;
using Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            Helpers.CheckIfExistSalt();
            this.LoadTree();
            this.tvGroups.SelectedItemChanged += this.TvGroups_SelectedItemChanged;
        }

        private void TvGroups_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            object tag = ((e.NewValue as TreeViewItem).Tag);
            this.Model.GroupSelectedObject = tag;

            this.Reset();
            ReloadCredentialsSource();
        }

        private void Reset()
        {
            this.Model.ClearNewCredential();
            this.txtNewPassword.Password = string.Empty;
            this.Model.ClearNewGroup();
            this.Model.ClearModifyCredential();
            this.Model.ClearModifyGroup();
            this.Model.ClearShare();
        }

        private void ShowHidePassword(object sender, RoutedEventArgs e)
        {
            Credential row = (sender as Button).DataContext as Credential;
            row.ShowPassword = !row.ShowPassword;
            this.Model.ReloadCredentialsGrid();
        }

        private void ReloadCredentialsSource()
        {
            this.Model.CredentialsSource = Credentials.LoadCredentialsLocalByGroupID(this.Model.CredentialGroupID.GetValueOrDefault());

        }

        private void BtnGroupAdd_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
            this.Model.AddGroup = !this.Model.AddGroup;
            this.Model.AddCredential = false;
        }

        private void BtnCredentialAdd_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
            this.Model.AddCredential = !this.Model.AddCredential;
            this.Model.AddGroup = false;
        }

        private void BtnSaveGroup_Click(object sender, RoutedEventArgs e)
        {
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
            try
            {
                if (this.Model.ModifyCredential)
                    Credentials.Modify(this.Model.ModifyCredentialID, this.Model.CredentialGroupID.GetValueOrDefault(), this.Model.NewCredentialTitle, this.Model.NewCredentialUsername, this.Model.NewCredentialEmail, this.Model.NewCredentialPassword, this.Model.NewCredentialUrl, this.Model.NewCredentialNotes, this.Model.NewCredentialExpires);
                else
                    Credentials.Add(this.Model.CredentialGroupID.GetValueOrDefault(), this.Model.NewCredentialTitle, this.Model.NewCredentialUsername, this.Model.NewCredentialEmail, this.Model.NewCredentialPassword, this.Model.NewCredentialUrl, this.Model.NewCredentialNotes, this.Model.NewCredentialExpires);
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
            this.Reset();
            if (!this.Model.ModifyCredential)
            {
                this.Model.ModifyCredentialID = this.Model.CredentialSelected.ID;
                this.Model.NewCredentialTitle = this.Model.CredentialSelected.Title;
                this.Model.NewCredentialUsername = this.Model.CredentialSelected.Username;
                this.Model.NewCredentialEmail = this.Model.CredentialSelected.Email;
                this.txtNewPassword.Password = this.Model.CredentialSelected.Password;
                this.Model.NewCredentialUrl = this.Model.CredentialSelected.Url;
                this.Model.NewCredentialExpires = this.Model.CredentialSelected.Expires.ToString();
            }
            this.Model.ModifyCredential = !this.Model.ModifyCredential;
        }

        private void BtnCredentialShare_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
            if (!this.Model.ShareCredential)
            {
                this.Model.CredentialSelected.ShowPassword = true;
                string hash = Credentials.Export(this.Model.CredentialSelected.ID);
                this.Model.ShareCredentialString = hash;
                this.Model.CredentialSelected.ShowPassword = false;
            }
            this.Model.ShareCredential = !this.Model.ShareCredential;
        }

        private void BtnCredentialImport_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
            try
            {
                Credentials.Import(this.Model.GroupSelected?.ID);
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
            Credentials.CreateBackup();
            MessageBox.Show($"Backup creato in {Path.Combine(Directory.GetCurrentDirectory(), "backup")}.{Environment.NewLine}Affinchè possa essere reimportato correttamente, è necessario che la password impostata di default non cambi.");
        }

        private void BtnCredentialImportBackup_Click(object sender, RoutedEventArgs e)
        {
            int errors = Credentials.ImportBackup();
            string msgError = errors > 0 ? $" {errors} record non sono stati importati per errori." : string.Empty;
            MessageBox.Show($"Import completato.{msgError}");
            this.ReloadCredentialsSource();
        }

        private void BtnCredentialImportChrome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int errors = Credentials.ImportChrome(this.Model.GroupSelected?.ID);
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
    }
}
