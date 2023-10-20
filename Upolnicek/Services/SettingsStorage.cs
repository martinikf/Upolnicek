using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Shell;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Upolnicek
{
    internal class SettingsStorage : ISettingsStorage
    {
        public async Task<bool> SaveAsync(string server, string login, string password)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                WritableSettingsStore userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                    
                userSettingsStore.CreateCollection("Upolnicek");
                userSettingsStore.SetString("Upolnicek", "email", login);
                userSettingsStore.SetString("Upolnicek", "server", server);

                if(password != null)
                    userSettingsStore.SetString("Upolnicek", "password", Convert.ToBase64String(Protect(Encoding.UTF8.GetBytes(password))));
            }
            catch
            {
                //Couldn't save settings
                return false;
            }

            return true;
        }

        public async Task<Settings?> GetSettingsAsync()
        {
            var settings = new Settings();

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                if (userSettingsStore.CollectionExists("Upolnicek"))
                {
                    settings.Login = userSettingsStore.GetString("Upolnicek", "email");
                    settings.Server = userSettingsStore.GetString("Upolnicek", "server");

                    if (userSettingsStore.PropertyExists("Upolnicek", "password"))
                        settings.Password = Encoding.UTF8.GetString(Unprotect(
                            Convert.FromBase64String(userSettingsStore.GetString("Upolnicek", "password"))));
                }
            }
            catch
            {
                return null;
            }

            return settings;
        }

        public async Task ForgetPasswordAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                if (userSettingsStore.CollectionExists("Upolnicek"))
                {
                    if(userSettingsStore.PropertyExists("Upolnicek", "password"))
                        userSettingsStore.DeleteProperty("Upolnicek", "password");
                }
            }
            catch
            {
                //Password doesn't exist or couldn't be deleted
            }
        }

        private byte[] Protect(byte[] data)
        {
            try
            {
                return ProtectedData.Protect(data, new byte[] { 5, 4, 6 }, DataProtectionScope.CurrentUser);
            }
            catch
            {
                return null;
            }
        }

        private byte[] Unprotect(byte[] data)
        {
            try
            {
                return ProtectedData.Unprotect(data, new byte[] { 5, 4, 6 }, DataProtectionScope.CurrentUser);
            }
            catch
            {
                return null;
            }
        }
    }
}
