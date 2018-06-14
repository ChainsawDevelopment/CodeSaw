using System;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace Web.Modules.Api.Model
{
    public class Review
    {
        public virtual Guid Id { get; set; }
        public virtual int UserId { get; set; }
        public virtual Guid RevisionId { get; set; }
        public virtual DateTimeOffset ReviewedAt { get; set; }

        public virtual DateTimeOffset LastUpdatedAt { get; set; }
    }

    public class ReviewConfig : ClassMapping<Review>
    {
        public ReviewConfig()
        {
            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, v =>
            {
                v.Type(new DateTimeOffsetType());
            });

            Property(x => x.UserId);
            Property(x => x.RevisionId);
            Property(x => x.ReviewedAt);
        }
    }
}