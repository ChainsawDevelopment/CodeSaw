using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;
using RepositoryApi;

namespace Web.Modules.Api.Model
{
    public class Review
    {
        public virtual Guid Id { get; set; }
        public virtual int UserId { get; set; }
        public virtual Guid RevisionId { get; set; }
        public virtual DateTimeOffset ReviewedAt { get; set; }

        public virtual DateTimeOffset LastUpdatedAt { get; set; }

        public virtual ISet<PathPair> ReviewedFiles { get; set; }

        public Review()
        {
            ReviewedFiles = new HashSet<PathPair>();
        }

        public virtual void ReviewFiles(IReadOnlyList<PathPair> files)
        {
            var actualFiles = new HashSet<PathPair>(files);

            ReviewedFiles.IntersectWith(actualFiles);

            ReviewedFiles.UnionWith(actualFiles);
        }
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

            Set(x => x.ReviewedFiles,
                coll =>
                {
                    coll.Table("ReviewedFiles");
                    coll.Lazy(CollectionLazy.Lazy);
                    coll.Cascade(Cascade.DeleteOrphans);
                    coll.Key(key => { key.Column("ReviewId"); });
                },
                el =>
                {
                    
                }
            );
        }
    }

    public class PathPairConfig : ComponentMapping<PathPair>
    {
        public PathPairConfig()
        {
            Property(x=>x.OldPath);
            Property(x=>x.NewPath);
        }
    }
}