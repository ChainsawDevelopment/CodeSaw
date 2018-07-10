using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace Web.Modules.Api.Model
{
    public class FileCommentConfig : ClassMapping<FileComment>
    {
        public FileCommentConfig()
        {
            Table("FileDiscussions");
            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, v => { v.Type(new DateTimeOffsetType()); });

            Component(x => x.File);
            Property(x => x.LineNumber);

            Bag(x => x.Comments, s =>
            {
                s.Key(key =>
                {
                    key.NotNullable(true);
                    key.Column(c=>
                    {
                        c.Name("FileDiscussionId");
                    });
                });

                s.Cascade(Cascade.All.Include(Cascade.DeleteOrphans));
                s.Lazy(CollectionLazy.Lazy);
            }, c => c.OneToMany());
        }
    }
}