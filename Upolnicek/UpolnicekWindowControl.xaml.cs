﻿using EnvDTE;
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
        private int _selectedAssignmentId;
        

        public UpolnicekWindowControl()
        {
            this.InitializeComponent();

            _settingsStorage = new SettingsStorage();
            _api = new UpolnicekAPI();

            ServerUrlTextBox.Text = DEFAULT_SERVER;

            new Action(async () =>
            {
                await LoadSettingsAsync();
                if(!String.IsNullOrEmpty(PasswordPasswordBox.Password))
                    await LoginAsync();
            })();           
        }

        private async Task LoadSettingsAsync()
        {
            var settings = await _settingsStorage.GetSettingsAsync();
            if (settings != null)
            {
                _api.SetValues(settings.Value.Login, settings.Value.Password, settings.Value.Server);

                var server = settings.Value.Server;
                if(server.EndsWith("/"))
                    server = server.Substring(0, server.Length - 1);

                ServerUrlTextBox.Text = server;
                LoginTextBox.Text = settings.Value.Login;
                PasswordPasswordBox.Password = settings.Value.Password;
            }
        }

        private async void LoginButtonOnClick(object sender, RoutedEventArgs e)
        {
            _api.SetValues(LoginTextBox.Text, PasswordPasswordBox.Password, ServerUrlTextBox.Text);
            await LoginAsync();
        }

        private async Task LoginAsync()
        {
            if (await _api.AuthenticateAsync() == false)
            {
                LoginErrorLabel.Visibility = Visibility.Visible;
                return;
            }

            await SaveSettingsAsync();
            await ShowAssignmentsScreenAsync();
        }

        private async Task SaveSettingsAsync()
        {
            if (RememberLoginCheckBox.IsChecked == true)
                await _settingsStorage.SaveAsync(ServerUrlTextBox.Text, LoginTextBox.Text, PasswordPasswordBox.Password);
            else
                await _settingsStorage.SaveAsync(ServerUrlTextBox.Text, LoginTextBox.Text, null);
        }

        private async void AssignmentButtonOnClick(int id)
        {
            _selectedAssignmentId = id; 
            await ShowFileExplorerScreenAsync();
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

        private async void SignOutButtonOnClick(object sender, RoutedEventArgs e)
        {
            await _settingsStorage.ForgetPasswordAsync();
            await ShowLoginScreenAsync();
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

            var errors = await _api.HandInAssignmentAsync(_selectedAssignmentId, filesToUpload, projectPath);

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

        private async Task ShowFileExplorerScreenAsync(string message = "")
        {
            ResetGUI();

            var builder = new FileExplorerBuilder(HeadingLabel.Foreground);
            _fileExplorerTree = builder.Build(FileExplorerStackPanel, await GetProjectPathAsync());

            if(_fileExplorerTree == null)
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
        
        private async Task ShowAssignmentsScreenAsync()
        {
            ResetGUI();

            var builder = new AssignmentsBuilder(AssignmentButtonOnClick);
            if (!builder.Build(await _api.GetAssignmentsAsync(), AssignmentsStackPanel))
            {
                await ShowLoginScreenAsync("Nepodařilo se načíst seznam úkolů");
            }
            else
            {
                AssignmentsContainerStackPanel.Visibility = Visibility.Visible;
            }
        }

        private async Task ShowLoginScreenAsync(string message = "")
        {
            ResetGUI();

            await LoadSettingsAsync();

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