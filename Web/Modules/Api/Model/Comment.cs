using System;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace Web.Modules.Api.Model
{
    public enum CommentState
    {
        NoActionNeeded,
        NeedsResolution,
        Resolved
    }

    public class Comment
    {
        public virtual Guid Id { get; set; }
        public virtual Guid? ParentId { get; set; }
        public virtual Guid ReviewId { get; set; }
        public virtual string Content { get; set; }
        public virtual string FilePath { get; set; }
        public virtual string ChangeKey { get; set; }
        public virtual CommentState State { get; set; }
        public virtual DateTimeOffset LastUpdatedAt { get; set; }
        public virtual DateTimeOffset CreatedAt { get; set; }
    }

    public class CommentMapping : ClassMapping<Comment>
    {
        public CommentMapping()
        {
            Table("Comments");

            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, mapper =>
            {
                mapper.Type(new DateTimeOffsetType());
            });

            Property(x => x.Content, mapper => mapper.NotNullable(true));
            Property(x => x.CreatedAt, mapper => mapper.NotNullable(true));
            Property(x => x.FilePath, mapper => mapper.NotNullable(false));
            Property(x => x.ChangeKey, mapper => mapper.NotNullable(false));
            Property(x => x.State, mapper =>
            {
                mapper.Type<EnumStringType<CommentState>>();
                mapper.NotNullable(true);
            });
            Property(x => x.ParentId);
            Property(x => x.ReviewId);
        }
    }
}