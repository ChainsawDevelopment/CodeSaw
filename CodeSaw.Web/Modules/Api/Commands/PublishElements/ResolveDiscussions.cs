using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
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
            var discussionsToResolve = 
                _session.Query<Discussion>().Where(x => resolvedDiscussions.Contains(x.Id)).ToList();

            foreach (var comment in discussionsToResolve)
            {
                comment.State = CommentState.Resolved;

                await _session.SaveAsync(comment);
            }
        }
    }
}