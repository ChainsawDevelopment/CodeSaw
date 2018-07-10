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

    public class Comment<TReviewedItem>
    {
        public virtual Guid Id { get; set; }
        public virtual Guid? ParentId { get; set; }
        public virtual Guid ReviewedItemId { get; set; }
        public virtual string Content { get; set; }
        public virtual CommentState State { get; set; }
        public virtual DateTimeOffset LastUpdatedAt { get; set; }
        public virtual DateTimeOffset CreatedAt { get; set; }
    }

    public class ReviewCommentMapping : ClassMapping<Comment<Review>>
    {
        public ReviewCommentMapping()
        {
            Table("Comments");

            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, mapper =>
            {
                mapper.Type(new DateTimeOffsetType());
            });

            Property(x => x.Content, mapper => mapper.NotNullable(true));
            Property(x => x.CreatedAt, mapper => mapper.NotNullable(true));
            Property(x => x.State, mapper =>
            {
                mapper.Type<EnumStringType<CommentState>>();
                mapper.NotNullable(true);
            });
            Property(x => x.ParentId);
            Property(x => x.ReviewedItemId, p => p.Column("ReviewId"));
        }
    }

    public class FileCommentMapping : ClassMapping<Comment<FileComment>>
    {
        public FileCommentMapping()
        {
            Table("FileComments");

            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, mapper =>
            {
                mapper.Type(new DateTimeOffsetType());
            });

            Property(x => x.Content, mapper => mapper.NotNullable(true));
            Property(x => x.CreatedAt, mapper => mapper.NotNullable(true));
            Property(x => x.State, mapper =>
            {
                mapper.Type<EnumStringType<CommentState>>();
                mapper.NotNullable(true);
            });
            Property(x => x.ParentId);
            Property(x => x.ReviewedItemId, p => p.Column("FileDiscussionId"));
        }
    }
}