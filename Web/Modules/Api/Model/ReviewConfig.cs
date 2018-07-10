using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace Web.Modules.Api.Model
{
    public class ReviewConfig : ClassMapping<Review>
    {
        public ReviewConfig()
        {
            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, v => { v.Type(new DateTimeOffsetType()); });

            Property(x => x.UserId);
            Property(x => x.RevisionId);
            Property(x => x.ReviewedAt);

            Bag(x => x.Files, coll =>
            {
                coll.Table("ReviewFiles");
                coll.Key(key => key.Column("ReviewId"));
            });

            Bag(x => x.FileComments, e =>
            {
                e.Key(key =>
                {
                    key.NotNullable(true);
                    key.Column(c=>
                    {
                        c.Name("ReviewId");
                        c.NotNullable(true);
                    });
                });
                e.Cascade(Cascade.All.Include(Cascade.DeleteOrphans));
                e.Lazy(CollectionLazy.Lazy);
                //e.Inverse(true);
            }, c =>
            {
                c.OneToMany();
            });
        }
    }
    
    public class FileReviewConfig : ComponentMapping<FileReview>
    {
        public FileReviewConfig()
        {
            Component(x => x.File);
            Property(x => x.Status);
        }
    }
}