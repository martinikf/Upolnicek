using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Upolnicek.Builders;

namespace Upolnicek
{
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
#pragma warning disable VSTHRD100 // Avoid async void methods

    public partial class UpolnicekWindowControl : UserControl
    {
        private const string DEFAULT_SERVER = "https://upolnicek.inf.upol.cz";

        private readonly UpolnicekAPI _api; 
        private readonly SettingsStorage _settingsStorage;

        private FileExplorerTree _fileExplorerTree;

        private Assignment _selectedAssignment;


        public UpolnicekWindowControl()
        {
            this.InitializeComponent();

            _settingsStorage = new SettingsStorage();
            _api = new UpolnicekAPI();

            ServerUrlTextBox.Text = DEFAULT_SERVER;

            LoadSettings();
            if(!String.IsNullOrEmpty(PasswordPasswordBox.Password))
                _ = LoginAsync();          
        }

        private void LoadSettings()
        {
            try
            {
                Settings? settings = _settingsStorage.GetSettings();

                if (settings != null)
                {
                    _api.SetValues(settings.Value.Login, settings.Value.Password, settings.Value.Server);

                    var server = settings.Value.Server;
                    if (server.EndsWith("/"))
                        server = server.Substring(0, server.Length - 1);

                    ServerUrlTextBox.Text = server;
                    LoginTextBox.Text = settings.Value.Login;
                    PasswordPasswordBox.Password = settings.Value.Password;
                }
            }
            catch { }
        }

        private async Task LoginAsync()
        {
            if (await _api.AuthenticateAsync() == false)
            {
                LoginErrorLabel.Visibility = Visibility.Visible;
                return;
            }

            SaveSettings();
            await ShowAssignmentsScreenAsync();
        }

        private void SaveSettings()
        {
            if (RememberLoginCheckBox.IsChecked == true)
                _settingsStorage.SaveSettings(ServerUrlTextBox.Text, LoginTextBox.Text, PasswordPasswordBox.Password);
            else
                _settingsStorage.SaveSettings(ServerUrlTextBox.Text, LoginTextBox.Text, null);
        }

        private async Task<string> GetProjectPathAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                DTE dte = (DTE)Package.GetGlobalService(typeof(DTE));
                return Path.GetDirectoryName(dte.Solution.FullName);
            }
            catch
            {
                return "";
            }
        }

        private async void LoginButtonOnClick(object sender, RoutedEventArgs e)
        {
            _api.SetValues(LoginTextBox.Text, PasswordPasswordBox.Password, ServerUrlTextBox.Text);
            await LoginAsync();
        }

        private void SignOutButtonOnClick(object sender, RoutedEventArgs e)
        {
            _settingsStorage.ForgetPassword();
            ShowLoginScreen();
        }

        private async void AssignmentButtonOnClick(Assignment ass)
        {
            _selectedAssignment = ass;

            await ShowFileExplorerScreenAsync();
        }

        private async void ReturnButtonOnClick(object sender, RoutedEventArgs e)
        {
            await ShowAssignmentsScreenAsync();
        }

        private async void HandInButtonOnClick(object sender, RoutedEventArgs e)
        {
            ShowUploadScreen();

            var projectPath = await GetProjectPathAsync();
            var filesToUpload = _fileExplorerTree.ToList()
                                .Where(x => !x.IsDirectory && x.CheckBox.IsChecked == true)
                                .Select(i => i.CheckBox.FullPath.Replace(projectPath, ""));

            if (!filesToUpload.Any())
            {
                await ShowFileExplorerScreenAsync("Nebyly vybrány žádné soubory");
                return;
            }

            var errors = await _api.HandInAssignmentAsync(_selectedAssignment.Id, filesToUpload, projectPath);

            if (errors.Count() == 0)
            {
                ShowResultScreen(filesToUpload, "Všechny soubory byly úspěšně odevzdány");
            }
            else
            {
                ShowResultScreen(errors, "Následující soubory se nepodařilo odevzdat");
            }
        }
       
        private async void ReturnFromResultButtonOnClick(object sender, RoutedEventArgs e)
        {
            await ShowAssignmentsScreenAsync();
        }

        private void ShowLoginScreen(string message = "")
        {
            ResetGUI();

            LoadSettings();

            if (!String.IsNullOrEmpty(message))
            {
                LoginContainerStackPanel.Children.Add(new Label
                {
                    Content = message,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = HeadingLabel.Foreground,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }

            LoginContainerStackPanel.Visibility = Visibility.Visible;
        }

        private async Task ShowAssignmentsScreenAsync()
        {
            ResetGUI();

            var builder = new AssignmentsBuilder(AssignmentButtonOnClick);
            if (!builder.Build(await _api.GetAssignmentsAsync(), AssignmentsStackPanel))
            {
                ShowLoginScreen("Nepodařilo se načíst seznam úkolů");
            }
            else
            {
                AssignmentsContainerStackPanel.Visibility = Visibility.Visible;
            }
        }

        private async Task ShowFileExplorerScreenAsync(string message = "")
        {
            ResetGUI();

            var builder = new FileExplorerBuilder(HeadingLabel.Foreground);
            _fileExplorerTree = builder.Build(FileExplorerStackPanel, await GetProjectPathAsync());

            AssignmentInfoTextBlock.Text =
                _selectedAssignment.CourseName + " : " + _selectedAssignment.Name + "\nOdevzdání do: " + _selectedAssignment.Deadline;

            //TODO: Add button to append description?
            //Problems with text wrapping
            //AssignmentInfoTextBlock.Text += "\n" + _selectedAssignment.Description;

            if (_fileExplorerTree == null)
            {
                await ShowAssignmentsScreenAsync();
                return;
            }

            if (!String.IsNullOrEmpty(message))
                FileExplorerStackPanel.Children.Add(new Label
                {
                    Content = message,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = HeadingLabel.Foreground,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

            FileExplorerContainerStackPanel.Visibility = Visibility.Visible;
        }

        private void ShowUploadScreen()
        {
            ResetGUI();

            HandInButton.IsEnabled = false;
            ReturnButton.IsEnabled = false;

            FileExplorerStackPanel.Children.Add(new Label
            {
                Content = "Probíhá odevzdávání...",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = HeadingLabel.Foreground,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            FileExplorerContainerStackPanel.Visibility = Visibility.Visible;
        }

        private void ShowResultScreen(IEnumerable<string> files, string message)
        {
            ResetGUI();

            var resultBuilder = new ResultLogBuilder(HeadingLabel.Foreground);
            if(!resultBuilder.Build(ResultStackPanel, files, message))
            {
                //Unexpected error -> shouldn't happen
            }

            ResultContainerStackPanel.Visibility = Visibility.Visible;
        }

        private void ResetGUI()
        {
            //Text boxes
            PasswordPasswordBox.Password = "";

            //StackPanels with content
            AssignmentsStackPanel.Children.Clear();
            FileExplorerStackPanel.Children.Clear();
            ResultStackPanel.Children.Clear();

            //Containers
            ResultContainerStackPanel.Visibility = Visibility.Collapsed;
            AssignmentsContainerStackPanel.Visibility = Visibility.Collapsed;
            FileExplorerContainerStackPanel.Visibility = Visibility.Collapsed;
            LoginContainerStackPanel.Visibility = Visibility.Collapsed;

            //Error labels
            LoginErrorLabel.Visibility = Visibility.Collapsed;

            //Buttons
            HandInButton.IsEnabled = true;
            ReturnButton.IsEnabled = true;
        }
    }
}