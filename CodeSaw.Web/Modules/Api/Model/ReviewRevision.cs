using System;
using System.Collections.Generic;
using CodeSaw.RepositoryApi;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace CodeSaw.Web.Modules.Api.Model
{
    public class ReviewRevision 
    {
        public virtual Guid Id { get; set; }
        public virtual ReviewIdentifier ReviewId { get; set; }
        public virtual int RevisionNumber { get; set; }
        public virtual string HeadCommit { get; set; }
        public virtual string BaseCommit { get; set; }

        public virtual IList<RevisionFile> Files { get; set; } = new List<RevisionFile>();

        public virtual DateTimeOffset LastUpdatedAt { get; set; }
    }

    public class RevisionFile
    {
        public virtual Guid Id { get; set; }
        public virtual PathPair File { get; set; }
        public virtual bool IsNew { get; set; }
        public virtual bool IsDeleted { get; set; }
        public virtual bool IsRenamed { get; set; }
        public virtual DateTimeOffset LastUpdatedAt { get; set; }

        public static RevisionFile FromDiff(FileDiff file)
        {
            return new RevisionFile
            {
                Id = GuidComb.Generate(),
                File = file.Path,
                IsNew = file.NewFile,
                IsDeleted = file.DeletedFile,
                IsRenamed = file.RenamedFile
            };
        }
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

            Bag(x => x.Files, coll =>
            {
                coll.Key(key =>
                {
                    key.Column(c=>
                    {
                        c.Name("RevisionId");
                        c.NotNullable(true);
                    });
                });
                coll.Cascade(Cascade.All);
            }, map =>
            {
                map.OneToMany();
            });
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

    public class RevisionFileMapping : ClassMapping<RevisionFile>
    {
        public RevisionFileMapping()
        {
            Table("RevisionFiles");

            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, v =>
            {
                v.Type(new DateTimeOffsetType());
            });

            Component(x => x.File);

            Property(x => x.IsNew);
            Property(x => x.IsDeleted);
            Property(x => x.IsRenamed);
        }
    }
}