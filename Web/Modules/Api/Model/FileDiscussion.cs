using System;
using RepositoryApi;

namespace Web.Modules.Api.Model
{
    public class FileDiscussion
    {
        public virtual Guid Id { get; set; }
        public virtual Guid RevisionId { get; set; }
        public virtual PathPair File { get; set; }
        public virtual int LineNumber { get; set; }
        public virtual Comment RootComment { get; set; }
        public virtual DateTimeOffset LastUpdatedAt { get; set; }
    }
}