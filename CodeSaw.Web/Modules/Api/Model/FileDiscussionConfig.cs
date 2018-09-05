using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace CodeSaw.Web.Modules.Api.Model
{
    public class FileDiscussionConfig : ClassMapping<FileDiscussion>
    {
        public FileDiscussionConfig()
        {
            Table("FileDiscussions");
            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, v => { v.Type(new DateTimeOffsetType()); });

            Property(x => x.RevisionId);

            Component(x => x.File);
            Property(x => x.LineNumber);

            ManyToOne(x => x.RootComment, mto =>
            {
                mto.Unique(true);

                mto.NotNullable(true);
                mto.Column(c=>
                {
                    c.NotNullable(true);
                    c.Name("RootCommentId");
                });

                mto.Class(typeof(Comment));
                mto.Cascade(Cascade.All.Include(Cascade.DeleteOrphans));
            });
        }
    }
}