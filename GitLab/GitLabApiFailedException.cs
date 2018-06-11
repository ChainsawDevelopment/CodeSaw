using System;

namespace GitLab
{
    public class GitLabApiFailedException : Exception
    {
        public GitLabApiFailedException(string message) : base(message)
        {

        }
    }
}
