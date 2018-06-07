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

        public class Item
        {
            public Guid Id { get; set; }
            public string Author { get; set; }
            public string Content { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public IEnumerable<Item> Children { get; set; }
        }

        public GetCommentList(int projectId, int reviewId)
        {
            _reviewId = new ReviewIdentifier(projectId, reviewId);
        }

        public async Task<IEnumerable<Item>> Execute(ISession session)
        {
            var comments = await (
                from c in session.Query<Comment>()
                orderby c.CreatedAt
                where c.ReviewId == _reviewId
                select c
            ).ToListAsync();

            return comments.Where(x => x.Parent == null).Select(MapComment).ToArray();
        }

        private static Item MapComment(Comment comment)
        {
            return new Item
            {
                Id = comment.Id,
                Author = comment.User.UserName,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                Children = comment.Children.Select(MapComment)
            };
        }
    }
}