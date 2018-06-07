using System;

namespace GitLab
{
    public class GitLabRefAlreadyExistsException : GitLabApiFailedException
    {
        public int ProjectId { get; }
        public string RefName { get; }
        public string Commit { get; }

        public GitLabRefAlreadyExistsException(int projectId, string refName, string commit)
            :base ($"Tried to create ref {refName} for project {projectId} and commit {commit}, but such ref already exists.")
        {
            ProjectId = projectId;
            RefName = refName;
            Commit = commit;
        }
    }
}