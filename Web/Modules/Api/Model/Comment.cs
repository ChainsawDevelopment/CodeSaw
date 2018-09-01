using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace Web.Modules.Api.Model
{
    [JsonConverter(typeof(StringEnumConverter), false)]
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

        public virtual Guid PostedInReviewId { get; set; }

        public virtual string Content { get; set; }
        public virtual DateTimeOffset LastUpdatedAt { get; set; }
        public virtual DateTimeOffset CreatedAt { get; set; }
    }

    public class ReviewCommentMapping : ClassMapping<Comment>
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
            Property(x => x.ParentId);
            Property(x => x.PostedInReviewId, c => c.Column("ReviewId"));
        }
    }
}