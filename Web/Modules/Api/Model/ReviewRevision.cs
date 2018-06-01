using System;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;
using RepositoryApi;

namespace Web.Modules.Api.Model
{
    public class ReviewRevision
    {
        public virtual Guid Id { get; set; }
        public virtual ReviewIdentifier ReviewId { get; set; }
        public virtual int RevisionNumber { get; set; }
        public virtual string HeadCommit { get; set; }
        public virtual string BaseCommit { get; set; }

        public virtual DateTimeOffset LastUpdatedAt { get; set; }
    }

    public class ReviewRevisionMapping : ClassMapping<ReviewRevision>
    {
        public ReviewRevisionMapping()
        {
            Table("Revisions");

            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, v =>
            {
                v.Type(new DateTimeOffsetType());
            });

            Component(x => x.ReviewId);
            Property(x => x.RevisionNumber);
            Property(x => x.HeadCommit, p => p.Length(40));
            Property(x => x.BaseCommit, p => p.Length(40));
        }
    }

    public class ReviewIdenfierMapping : ComponentMapping<ReviewIdentifier>
    {
        public ReviewIdenfierMapping()
        {
            Property(x => x.ProjectId);
            Property(x => x.ReviewId);
        }
    }
}