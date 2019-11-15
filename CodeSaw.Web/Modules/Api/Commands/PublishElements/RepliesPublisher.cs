using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.Web.Modules.Api.Model;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class RepliesPublisher
    {
        private readonly ISessionAdapter _sessionAdapter;

        public RepliesPublisher(ISessionAdapter sessionAdapter)
        {
            _sessionAdapter = sessionAdapter;
        }

        public async Task Publish(List<Item> replies, Review review, Dictionary<string, Guid> newCommentsMap)
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

                    Guid parentId;
                    if (newCommentsMap.TryGetValue(item.ParentId, out var savedParentId))
                    {
                        parentId = savedParentId;
                    }
                    else
                    {
                        parentId = Guid.Parse(item.ParentId);
                    }

                    _sessionAdapter.Save(new Comment
                    {
                        Id = id,
                        PostedInReviewId = review.Id,
                        Content = item.Content,
                        CreatedAt = DateTimeOffset.UtcNow,
                        ParentId = parentId
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