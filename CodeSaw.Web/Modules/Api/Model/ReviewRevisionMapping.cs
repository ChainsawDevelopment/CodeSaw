using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace CodeSaw.Web.Modules.Api.Model
{
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
            Property(x => x.ArchiveState);

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