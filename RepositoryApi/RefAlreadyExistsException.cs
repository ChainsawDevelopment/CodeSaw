using System;

namespace RepositoryApi
{
    public class RefAlreadyExistsException : Exception
    {
        public int ProjectId { get; }
        public string RefName { get; }
        public string Commit { get; }

        public RefAlreadyExistsException(int projectId, string refName, string commit)
            :base ($"Tried to create ref {refName} for project {projectId} and commit {commit}, but such ref already exists.")
        {
            ProjectId = projectId;
            RefName = refName;
            Commit = commit;
        }
    }
}