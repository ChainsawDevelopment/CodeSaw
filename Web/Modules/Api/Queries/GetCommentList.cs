using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using RepositoryApi;
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

            public Handler(ISession session)
            {
                _session = session;
            }

            public async Task<IEnumerable<Item>> Execute(GetCommentList query)
            {
                var comments = await (
                        from c in _session.Query<Comment>()
                        orderby c.CreatedAt
                        where c.ReviewId == query._reviewId
                        select c
                    )
                    .Fetch(x => x.User)
                    .FetchMany(x => x.Children)
                    .ToListAsync();

                return comments.Where(x => x.Parent == null).Select(MapComment).ToArray();
            }

            private static Item MapComment(Comment comment)
            {
                return new Item
                {
                    Id = comment.Id,
                    Author = comment.User.UserName,
                    Content = comment.Content,
                    State = comment.State.ToString(),
                    CreatedAt = comment.CreatedAt,
                    Children = comment.Children.Select(MapComment)
                };
            }
        }
    }
}