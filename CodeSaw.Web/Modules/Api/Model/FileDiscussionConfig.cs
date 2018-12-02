using NHibernate.Mapping.ByCode.Conformist;

namespace CodeSaw.Web.Modules.Api.Model
{
    public class FileDiscussionConfig : UnionSubclassMapping<FileDiscussion>
    {
        public FileDiscussionConfig()
        {
            Table("FileDiscussions");
            Component(x => x.File);
            Property(x => x.LineNumber);
            Property(x => x.FileId);
        }
    }
}