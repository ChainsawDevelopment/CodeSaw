using System;
using System.Collections.Generic;
using System.Linq;
using CodeSaw.RepositoryApi;

namespace CodeSaw.Web.Modules.Api.Model
{
    public class Review
    {
        public virtual Guid Id { get; set; }
        public virtual int UserId { get; set; }
        public virtual Guid RevisionId { get; set; }
        public virtual DateTimeOffset ReviewedAt { get; set; }

        public virtual DateTimeOffset LastUpdatedAt { get; set; }

        public virtual IList<FileReview> Files { get; set; }

        public Review()
        {
            Files = new List<FileReview>();
        }
    }

    public class FileReview
    {
        public Guid FileId { get; private set; }
        public PathPair File { get; private set; }
        public FileReviewStatus Status { get; set; }

        public FileReview(PathPair file, Guid fileId)
        {
            File = file;
            FileId = fileId;
        }

        protected FileReview()
        {
            
        }
    }

    public enum FileReviewStatus
    {
        Reviewed = 1
    }
}