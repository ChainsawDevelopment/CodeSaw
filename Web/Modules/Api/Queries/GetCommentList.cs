using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetCommentList : IQuery<IEnumerable<GetCommentList.Item>>
    {
        private readonly ReviewIdentifier _reviewId;

        public GetCommentList(int projectId, int reviewId)
        {
            _reviewId = new ReviewIdentifier(projectId, reviewId);
        }

        public class Item
        {
            public Guid Id { get; set; }
            public string Author { get; set; }
            public string Content { get; set; }
            public string State { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public IEnumerable<Item> Children { get; set; }
        }

        public class Handler : IQueryHandler<GetCommentList, IEnumerable<Item>>
        {
            private readonly ISession _session;

            private class ItemWithParent : Item
            {
                public Guid? ParentId { get; set; }
            }

            public Handler(ISession session)
            {
                _session = session;
            }

            public async Task<IEnumerable<Item>> Execute(GetCommentList query)
            {
                var comments = await (
                        from comment in _session.Query<Comment>()
                        join review in _session.Query<Review>() on comment.PostedInReviewId equals review.Id
                        join revision in _session.Query<ReviewRevision>() on review.RevisionId equals revision.Id
                        join user in _session.Query<ReviewUser>() on review.UserId equals user.Id
                        orderby comment.CreatedAt
                        where revision.ReviewId == query._reviewId
                        select new ItemWithParent
                        {
                            Id = comment.Id,
                            Content = comment.Content,
                            CreatedAt = comment.CreatedAt,
                            Author = user.UserName,
                            State = comment.State.ToString(),
                            ParentId = comment.ParentId
                        }
                    )
                    .ToListAsync();

                foreach (var comment in comments)
                {
                    JoinComments(comment, comments);
                }

                return comments.Where(comment => comment.ParentId == null).ToList();
            }

            private static void JoinComments(Item comment, IEnumerable<ItemWithParent> comments)
            {
                comment.Children = comments.Where(x => x.ParentId == comment.Id).ToList();
            }
        }
    }
}