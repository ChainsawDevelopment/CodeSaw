using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class MarkFilesPublisher
    {
        private readonly ISession _session;
        private readonly FindReviewDelegate _reviewForRevision;

        public MarkFilesPublisher(ISession session, FindReviewDelegate reviewForRevision)
        {
            _session = session;
            _reviewForRevision = reviewForRevision;
        }

        public async Task MarkFiles(Dictionary<RevisionId, List<PathPair>> reviewedFiles, Dictionary<RevisionId, List<PathPair>> unreviewedFiles)
        {
            foreach (var (revisionId, files) in reviewedFiles)
            {
                var review = _reviewForRevision(revisionId);
                var toAdd = files.Where(x => !review.Files.Any(y => y.File == x)).ToList();

                if (toAdd.Any())
                {
                    review.Files.AddRange(toAdd.Select(x => new FileReview(x) {Status = FileReviewStatus.Reviewed}));
                    await _session.SaveAsync(review);
                }
            }

            foreach (var (revisionId, files) in unreviewedFiles)
            {
                var review = _reviewForRevision(revisionId);
                var toRemove = review.Files.Where(x => files.Contains(x.File)).ToList();

                if (toRemove.Any())
                {
                    review.Files.RemoveRange(toRemove);

                    await _session.SaveAsync(review);
                }
            }
        }
    }
}