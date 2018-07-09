using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace Web.Modules.Api.Model
{
    public class FileCommentConfig : ClassMapping<FileComment>
    {
        public FileCommentConfig()
        {
            Id(x => x.Id, id => id.Generator(Generators.Assigned));
            Version(x => x.LastUpdatedAt, v => { v.Type(new DateTimeOffsetType()); });

            Component(x => x.File);
            Property(x => x.LineNumber);

            
            Bag(x => x.Comments, coll =>
            {
                coll.Table("Comments");
                coll.Key(key => key.Column("FileCommentId"));
            });
        }
    }
}