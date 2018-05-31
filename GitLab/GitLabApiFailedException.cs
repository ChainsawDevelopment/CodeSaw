using System;

namespace GitLab
{
    internal class GitLabApiFailedException : Exception
    {
        public GitLabApiFailedException(string message) : base(message)
        {

        }
    }
}
