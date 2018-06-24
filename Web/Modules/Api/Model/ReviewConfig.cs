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