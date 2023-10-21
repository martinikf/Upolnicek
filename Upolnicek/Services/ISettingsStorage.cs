using System.Threading.Tasks;

namespace Upolnicek
{
    internal interface ISettingsStorage
    {
        bool SaveSettings(string server, string login, string password);

        Settings? GetSettings();

        void ForgetPassword();
    }
}