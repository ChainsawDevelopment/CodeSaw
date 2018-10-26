using System;

namespace CodeSaw.RepositoryApi
{
    public class ExistingRefConflictException : Exception
    {
        public int ProjectId { get; }
        public string RefName { get; }
        public string Commit { get; }

        public ExistingRefConflictException(int projectId, string refName, string commit)
            :base ($"Tried to create ref {refName} for project {projectId} and commit {commit}. Such ref already exists, but has different value.")
        {
            ProjectId = projectId;
            RefName = refName;
            Commit = commit;
        }
    }
}