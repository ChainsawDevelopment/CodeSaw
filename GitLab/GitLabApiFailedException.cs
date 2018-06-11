using System;
using RestSharp;

namespace GitLab
{
    public class GitLabApiFailedException : Exception
    {
        public GitLabApiFailedException(string message) : base(message)
        {

        }


        public GitLabApiFailedException(IRestRequest request, IRestResponse response) : base($"Request {request.Method} {request.Resource} failed with {(int)response.StatusCode} {response.StatusDescription}\nError: {response.ErrorMessage}")
        {
        }
    }
}
