using System.Collections.Generic;
using System.Threading.Tasks;

namespace Upolnicek
{
    internal interface IServerAPI
    {
        void SetValues(string login, string password, string server);

        Task<bool> AuthenticateAsync();

        Task<IEnumerable<Assignment>> GetAssignmentsAsync();

        Task<IEnumerable<string>> HandInAssignmentAsync(int assignmentId, IEnumerable<string> filePaths, string projectPath);
    }
}
