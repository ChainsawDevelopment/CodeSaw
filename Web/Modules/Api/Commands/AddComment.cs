using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NHibernate.Linq;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Model;
using ISession = NHibernate.ISession;

namespace Web.Modules.Api.Commands
{
    public class AddComment : ICommand
    {
        public int ProjectId { get; set; }
        public int ReviewId { get; set; }
        public Guid? ParentId { get; set; }
        public string Content { get; set; }
        public bool NeedsResolution { get; set; }

        public class Handler : CommandHandler<AddComment>
        {
            private readonly ISession _session;
            private readonly ReviewUser _currentUser;

            public Handler(ISession session, [CurrentUser]ReviewUser currentUser)
            {
                _session = session;
                _currentUser = currentUser;
            }

            public override async Task Handle(AddComment command)
            {
                var parent = command.ParentId != null
                    ? await _session.Query<Comment>().FirstAsync(x => x.Id == command.ParentId)
                    : null;

                await _session.SaveAsync(new Comment
                {
                    Id = GuidComb.Generate(),
                    ReviewId = new ReviewIdentifier(command.ProjectId, command.ReviewId),
                    CreatedAt = DateTimeOffset.UtcNow,
                    User = _currentUser,
                    Parent = parent,
                    Content = command.Content,
                    State = command.NeedsResolution ? CommentState.NeedsResolution : CommentState.NoActionNeeded,
                    Children = new List<Comment>()
                });
            }
        }
    }
}