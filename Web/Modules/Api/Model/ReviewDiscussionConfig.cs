using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace Web.Modules.Api.Model
{
    public class ReviewDiscussionConfig : UnionSubclassMapping<ReviewDiscussion>
    {
        public ReviewDiscussionConfig()
        {
            Table("ReviewDiscussions");
            //Id(x => x.Id, id => id.Generator(Generators.Assigned));
            //Version(x => x.LastUpdatedAt, v => { v.Type(new DateTimeOffsetType()); });

            //Property(x => x.RevisionId);

            //ManyToOne(x => x.RootComment, mto =>
            //{
            //    mto.Unique(true);

            //    mto.NotNullable(true);
            //    mto.Column(c=>
            //    {
            //        c.NotNullable(true);
            //        c.Name("RootCommentId");
            //    });

            //    mto.Class(typeof(Comment));
            //    mto.Cascade(Cascade.All.Include(Cascade.DeleteOrphans));
            //});

            //Property(x => x.State, mapper =>
            //{
            //    mapper.Type<EnumStringType<CommentState>>();
            //    mapper.NotNullable(true);
            //});
        }
    }
}