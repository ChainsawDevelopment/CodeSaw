using System;

namespace Web.Modules.Api.Model
{
    public abstract class Discussion
    {
        public virtual CommentState State { get; set; }
        public virtual Guid Id { get; set; }
        public virtual Guid RevisionId { get; set; }
        public virtual Comment RootComment { get; set; }
        public virtual DateTimeOffset LastUpdatedAt { get; set; }
    }
}