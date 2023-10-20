using System.Threading.Tasks;

namespace Upolnicek
{
    internal interface ISettingsStorage
    {
        Task<bool> SaveAsync(string server, string login, string password);

        Task<Settings?> GetSettingsAsync();

        Task ForgetPasswordAsync();
    }
}