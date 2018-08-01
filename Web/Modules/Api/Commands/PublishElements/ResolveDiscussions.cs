using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Criterion;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Commands.PublishElements
{
    public class ResolveDiscussions
    {
        private readonly ISession _session;
        private readonly FindReviewDelegate _reviewForRevision;

        public ResolveDiscussions(ISession session, FindReviewDelegate reviewForRevision)
        {
            _session = session;
            _reviewForRevision = reviewForRevision;
        }

        public async Task Publish(List<Guid> resolvedDiscussions)
        {
            var commentsToResolve = _session.Query<Comment>().Where(x => resolvedDiscussions.Contains(x.Id)).ToList();

            foreach (var comment in commentsToResolve)
            {
                comment.State = CommentState.Resolved;

                await _session.SaveAsync(comment);
            }
        }
    }
}