using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Upolnicek
{
    internal class UpolnicekAPI : IServerAPI
    {
        private string _serverUrl;       
        private string _login;
        private string _password;

        private string _token;
        private int _userId;

        public UpolnicekAPI()
        {
        }

        public void SetValues(string login, string password, string server)
        {
            _serverUrl = server;
            _login = login;
            _password = password;
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
                }

                return _token != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsAsync()
        {
            if(_token == null)
                return null;

            IEnumerable<Assignment> assignments = null;

            try
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri(_serverUrl + "/api/assignments/user" + "?userId=" + _userId);
                client.DefaultRequestHeaders.Accept.Clear();

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
                client.DefaultRequestHeaders.Add("Cookie", "token=" + _token);
   
                await ExtendTokenValidityAsync();

                var response = (await client.GetAsync("")).Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(await response);

                await Task.Run(() => assignments = Parse(jsonDoc));
            }
            catch
            {
                //Couldn't load tasks / parse json
            }

            return assignments;
        }

        private IEnumerable<Assignment> Parse(JsonDocument jsonDoc)
        {
            var assignments = new List<Assignment>();

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

            return assignments;
        }

        public async Task<IEnumerable<string>> HandInAssignmentAsync(int assignmentId, IEnumerable<string> filePaths, string projectPath)
        {
            if (_token == null)
                return null;

            var error = new HashSet<string>();

            try
            {
                using (var client = new HttpClient())
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

                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
                    client.DefaultRequestHeaders.Add("Cookie", "token=" + _token);

                    var url = _serverUrl + "/api/version/add" + "?assignmentId=" + assignmentId;
                    formData.Add(new StringContent(assignmentId.ToString()), "assignmentId");

                    await ExtendTokenValidityAsync();

                    try
                    {
                        var response = await client.PostAsync(url, formData);

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
            //TODO: rewrite to use api call for extending token validity instead of authenticating again
            await AuthenticateAsync(); //extending token validity
        }
    }
}
