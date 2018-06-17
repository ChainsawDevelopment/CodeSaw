using System;
using System.Collections.Generic;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;
using RepositoryApi;
using Web.Auth;

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
        public virtual Comment Parent { get; set; }
        public virtual ICollection<Comment> Children { get; set; }
        public virtual ReviewIdentifier ReviewId { get; set; }
        public virtual ReviewUser User { get; set; }
        public virtual string Content { get; set; }
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

            Component(x => x.ReviewId);
            Property(x => x.Content, mapper => mapper.NotNullable(true));
            Property(x => x.CreatedAt, mapper => mapper.NotNullable(true));
            Property(x => x.State, mapper => mapper.NotNullable(true));
            Property(x => x.State, mapper =>
            {
                mapper.Type<EnumStringType<CommentState>>();
                mapper.NotNullable(true);
            });

            ManyToOne(x => x.User, mapper =>
            {
                mapper.Column("UserId");
                mapper.NotNullable(true);
            });
            ManyToOne(x => x.Parent, mapper => mapper.Column("ParentId"));

            Bag(x => x.Children, mapper =>
            {
                mapper.Inverse(true);
                mapper.Key(k => k.Column("ParentId"));
                mapper.Lazy(CollectionLazy.NoLazy);
                mapper.Cascade(Cascade.All);
            },
            action => action.OneToMany());
        }
    }
}