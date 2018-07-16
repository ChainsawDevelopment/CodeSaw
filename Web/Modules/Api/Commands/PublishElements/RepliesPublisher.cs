using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Commands.PublishElements
{
    public class RepliesPublisher
    {
        private readonly ISession _session;

        public RepliesPublisher(ISession session)
        {
            _session = session;
        }

        public async Task Publish(List<Item> replies, Review review)
        {
            while (true)
            {
                var haveParent = replies.Where(x => !x.ParentId.StartsWith("REPLY-")).ToList();

                if (!haveParent.Any())
                {
                    return;
                }

                foreach (var item in haveParent)
                {
                    var id = GuidComb.Generate();

                    await _session.SaveAsync(new Comment
                    {
                        Id = id,
                        State = CommentState.NoActionNeeded,
                        PostedInReviewId = review.Id,
                        Content = item.Content,
                        CreatedAt = DateTimeOffset.UtcNow,
                        ParentId = Guid.Parse(item.ParentId)
                    });

                    var nowHaveParent = replies.Where(x => x.ParentId == item.Id);
                    foreach (var child in nowHaveParent)
                    {
                        child.ParentId = id.ToString();
                    }

                    replies.Remove(item);
                }
            }
        }

        public class Item
        {
            public string Id { get; set; }
            public string ParentId { get; set; }
            public string Content { get; set; }
        }
    }
}