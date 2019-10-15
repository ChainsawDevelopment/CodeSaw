using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.Web.Modules.Api.Model;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class ResolveDiscussions
    {
        private readonly ISessionAdapter _sessionAdapter;
        private readonly FindReviewDelegate _reviewForRevision;

        public ResolveDiscussions(ISessionAdapter sessionAdapter, FindReviewDelegate reviewForRevision)
        {
            _sessionAdapter = sessionAdapter;
            _reviewForRevision = reviewForRevision;
        }

        public async Task Publish(List<Guid> resolvedDiscussions)
        {
            var discussionsToResolve = _sessionAdapter.GetDiscussions(resolvedDiscussions);

            foreach (var comment in discussionsToResolve)
            {
                comment.State = CommentState.Resolved;

                _sessionAdapter.Save(comment);
            }
        }
    }
}