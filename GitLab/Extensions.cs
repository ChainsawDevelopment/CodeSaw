using System.Threading.Tasks;
using RestSharp;

namespace GitLab
{
    static class Extensions
    {
        public static async Task<T> Execute<T>(this IRestRequest request, IRestClient restClient)
        {
            var response =await restClient.ExecuteGetTaskAsync<T>(request);
            if (!response.IsSuccessful)
            {
                throw new GitLabApiFailedException($"Request {request.Method} {request.Resource} failed with {(int)response.StatusCode} {response.StatusDescription}\nError: {response.ErrorMessage}");
            }

            return response.Data;
        }

        public static async Task<string> ExecuteRaw(this IRestRequest request, IRestClient restClient)
        {
            var response =await restClient.ExecuteGetTaskAsync(request);
            if (!response.IsSuccessful)
            {
                throw new GitLabApiFailedException($"Request {request.Method} {request.Resource} failed with {(int)response.StatusCode} {response.StatusDescription}\nError: {response.ErrorMessage}");
            }

            return response.Content;
        }

        public static async Task<IRestResponse> Execute(this IRestRequest request, IRestClient client)
        {
            return await client.ExecuteTaskAsync(request);
        }
    }
}