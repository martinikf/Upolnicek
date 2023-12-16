using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Upolnicek.Data;

namespace Upolnicek
{
    internal class UpolnicekAPI : IServerAPI, IDisposable
    {
        private HttpClient _httpClient;

        private string _serverUrl;       
        private string _login;
        private string _password;

        private string _token;
        private int _userId;

        private DateTime tokenValidity = DateTime.Now;

        public UpolnicekAPI()
        {
            _httpClient = new HttpClient();
        }

        public void SetValues(string login, string password, string server)
        {
            _serverUrl = server;
            _login = login;
            _password = password;
        }

        private void UpdateClient()
        {
            if (_httpClient.BaseAddress is null)
            {
                _httpClient.BaseAddress = new Uri(_serverUrl);
            }
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
            _httpClient.DefaultRequestHeaders.Add("Cookie", "token=" + _token);
        }

        public async Task<bool> AuthenticateAsync()
        {
            if (String.IsNullOrEmpty(_serverUrl) || String.IsNullOrEmpty(_login) || String.IsNullOrEmpty(_password))
                return false;

            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(_serverUrl + "/api/user/log-in");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                var json = "{\"email\":\"" + _login + "\"," + "\"password\":\"" + _password + "\"}";

            
                using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync()))
                {
                    await streamWriter.WriteAsync(json);
                }

                var httpResponse = (HttpWebResponse) (await httpWebRequest.GetResponseAsync());
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEndAsync();
                    var jsonDoc = JsonDocument.Parse(await result);
                    JsonElement root = jsonDoc.RootElement;

                    _userId = root.GetProperty("user").GetProperty("id").GetInt32();
                    _token = root.GetProperty("token").GetString();
                    UpdateClient();
                }

                return _token != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<IEnumerable<Course>> GetCoursesAsync()
        {
            return await GetObjectsAsync<Course>("/api/courses/user" + "?userId=" + _userId, ParseCourses);
        }

        public async Task<IEnumerable<CourseTasks>> GetTasksAsync()
        {
            var cTasks = new List<CourseTasks>();
            foreach (var course in await GetCoursesAsync()) {
                cTasks.Add(new CourseTasks(course, await GetObjectsAsync<CourseTask>("/api/tasks/course" + "?courseId=" + course.Id, ParseTasks)));
            }
            return cTasks;
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsAsync()
        {
            return await GetObjectsAsync<Assignment>("/api/assignments/user" + "?userId=" + _userId, ParseAssignments);
        }

        private async Task<IEnumerable<T>> GetObjectsAsync<T> (string path, Func<JsonDocument, IEnumerable<T>> parser)
        {
            if (_token == null)
                return null;

            IEnumerable<T> values = null;
            try
            {
                var json = await GetRequestAsync(path);
                await Task.Run(() => values = parser(json));
            }
            catch
            {
                //Error
            }

            return values;
        }

        private async Task<JsonDocument> GetRequestAsync(string path, bool skipExtend = false)
        {
            try
            {
                if(!skipExtend)
                    await ExtendTokenValidityAsync();

                var response = (await _httpClient.GetAsync(path)).Content.ReadAsStringAsync();
                return JsonDocument.Parse(await response);
            }
            catch
            {
                await Console.Out.WriteLineAsync("Error GetRequestAsync: " + path);
                return null;
            }
        }

        public async Task<bool> AcceptTaskAsync(int taskId) {
            try
            {
                await ExtendTokenValidityAsync();

                var data = new
                {
                    taskId = taskId.ToString(),
                    studentId = _userId.ToString()
                };

                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/assignment/add", content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                //Couldn't load ass / parse json
                return false;
            }
        }

        private IEnumerable<Course> ParseCourses(JsonDocument jsonDoc) { 
            
            var courses = new List<Course>();

            try
            {
                foreach (var course in jsonDoc.RootElement.GetProperty("studentCourses").EnumerateArray())
                {
                    courses.Add(
                        new Course(
                            course.GetProperty("id").GetInt32(),
                            course.GetProperty("name").GetString()));
                }
            }
            catch
            {
                Console.WriteLine("Error while parsing courses json.");
            }

            return courses;
        }

        private IEnumerable<CourseTask> ParseTasks(JsonDocument jsonDoc) {

            //Ignores already accepted tasks -> not null in assignment property
            var tasks = new List<CourseTask>();

            try
            {
                foreach(var task in jsonDoc.RootElement.EnumerateArray())
                {
                    if(task.GetProperty("assignment").ValueKind != JsonValueKind.Null)
                        continue;

                    tasks.Add(
                        new CourseTask(
                            task.GetProperty("id").GetInt32(),
                            task.GetProperty("name").GetString()));
                }
            }
            catch
            {
                Console.WriteLine("Error while parsing tasks json");
            }

            return tasks; 
        }

        private IEnumerable<Assignment> ParseAssignments(JsonDocument jsonDoc)
        {
            var assignments = new List<Assignment>();

            try
            {
                foreach (var assignment in jsonDoc.RootElement.GetProperty("studentAssignments").EnumerateArray())
                {
                    assignments.Add(
                        new Assignment(
                            assignment.GetProperty("id").GetInt32(),
                            assignment.GetProperty("Task").GetProperty("name").GetString(),
                            assignment.GetProperty("Task").GetProperty("description").GetString(),
                            assignment.GetProperty("Task").GetProperty("deadline").GetString(),
                            assignment.GetProperty("Course").GetProperty("name").GetString()));
                }
            }
            catch
            {
                Console.WriteLine("Error while parsing assignments json.");
            }

            return assignments;
        }

        public async Task<IEnumerable<string>> HandInAssignmentAsync(int assignmentId, IEnumerable<string> filePaths, string projectPath)
        {
            if (_token == null)
                return null;

            var error = new HashSet<string>();

            try
            {
                using (var formData = new MultipartFormDataContent())
                {
                    foreach (var path in filePaths)
                    {
                        try
                        {
                            var fileContent = new StreamContent(File.OpenRead(projectPath + path));
                            formData.Add(fileContent, "files", path);
                        }
                        catch
                        {
                            error.Add(path);
                        }
                    }

                    await ExtendTokenValidityAsync();

                    var url = _serverUrl + "/api/version/add" + "?assignmentId=" + assignmentId;
                    formData.Add(new StringContent(assignmentId.ToString()), "assignmentId");

                    try
                    {
                        var response = await _httpClient.PostAsync(url, formData);

                        if (response.StatusCode != HttpStatusCode.OK)
                            error.UnionWith(filePaths);
                    }
                    catch
                    {
                        //couldn't send files
                        error.UnionWith(filePaths);
                    }

                    return error;
                }
            }
            catch 
            {
                return filePaths;
            }
        }
    
        private async Task ExtendTokenValidityAsync()
        {
            if (tokenValidity.AddHours(2) < DateTime.Now)
            {
                await AuthenticateAsync();
            }
            else
            {
                await GetRequestAsync("/api/auth/extendToken", true);
            }

            tokenValidity = DateTime.Now;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
