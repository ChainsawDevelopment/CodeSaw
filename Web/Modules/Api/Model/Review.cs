using System;
using System.Collections.Generic;
using System.Linq;
using RepositoryApi;
using Web.Auth;

namespace Web.Modules.Api.Model
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

        public virtual void ReviewFiles(IReadOnlyList<PathPair> allFiles, IReadOnlyList<PathPair> reviewedFiles)
        {
            var unreviewed = allFiles.Except(reviewedFiles);

            foreach (var file in reviewedFiles)
            {
                var status = Files.SingleOrDefault(x => x.File == file);

                if (status == null)
                {
                    status = new FileReview(file);
                    Files.Add(status);
                }

                status.Status = FileReviewStatus.Reviewed;
            }

            foreach (var file in unreviewed)
            {
                var status = Files.SingleOrDefault(x => x.File == file);

                if (status == null)
                {
                    status = new FileReview(file);
                    Files.Add(status);
                }

                status.Status = FileReviewStatus.Unreviewed;
            }
        }
    }

    public class FileReview
    {
        public PathPair File { get; private set; }
        public FileReviewStatus Status { get; set; }

        public FileReview(PathPair file)
        {
            File = file;
        }

        protected FileReview()
        {
            
        }
    }

    public enum FileReviewStatus
    {
        Reviewed = 1,
        [Obsolete]
        Unreviewed = 2
    }
}