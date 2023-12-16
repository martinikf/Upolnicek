using System.Collections.Generic;
using System.Threading.Tasks;
using Upolnicek.Data;

namespace Upolnicek
{
    internal interface IServerAPI
    {
        void SetValues(string login, string password, string server);

        Task<bool> AuthenticateAsync();

        Task<IEnumerable<CourseTasks>> GetTasksAsync();

        Task<bool> AcceptTaskAsync(int taskId);

        Task<IEnumerable<Assignment>> GetAssignmentsAsync();

        Task<IEnumerable<string>> HandInAssignmentAsync(int assignmentId, IEnumerable<string> filePaths, string projectPath);
    }
}
