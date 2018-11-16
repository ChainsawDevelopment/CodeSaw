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
}