using System;
using CodeSaw.RepositoryApi;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace CodeSaw.Web.Modules.Api.Model
{
    public class FileHistoryEntry
    {
        public virtual Guid Id { get; set; }
        public virtual Guid FileId { get; set; }
        public virtual Guid? RevisionId { get; set; }
        public virtual ReviewIdentifier ReviewId { get; set; }
        public virtual string FileName { get; set; }
 
        public virtual bool IsNew { get; set; }
        public virtual bool IsRenamed { get; set; }
        public virtual bool IsDeleted { get; set; }
        public virtual bool IsModified { get; set; }
    }

    public class FileHistoryEntryConfig : ClassMapping<FileHistoryEntry>
    {
        public FileHistoryEntryConfig()
        {
            Table("FileHistory");
            Schema("dbo");

            Id(x => x.Id, id => id.Generator(Generators.Assigned));

            Property(x => x.FileId);
            Property(x => x.RevisionId);
            Component(x => x.ReviewId);
            Property(x => x.FileName);

            Property(x => x.IsNew);
            Property(x => x.IsRenamed);
            Property(x => x.IsDeleted);
            Property(x => x.IsModified);
        }
    }
}