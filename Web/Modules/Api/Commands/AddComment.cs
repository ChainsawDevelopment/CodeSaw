using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
            private readonly IHttpContextAccessor _httpContextAccessor;
            private readonly IUserStore<ReviewUser> _userStore;

            public Handler(ISession session, IHttpContextAccessor httpContextAccessor, IUserStore<ReviewUser> userStore)
            {
                _session = session;
                _httpContextAccessor = httpContextAccessor;
                _userStore = userStore;
            }

            public override async Task Handle(AddComment command)
            {
                var httpContextUser = _httpContextAccessor.HttpContext.User;
                var user = await _userStore.FindByNameAsync(httpContextUser.Identity.Name, CancellationToken.None);

                var parent = command.ParentId != null
                    ? await _session.Query<Comment>().FirstAsync(x => x.Id == command.ParentId)
                    : null;

                await _session.SaveAsync(new Comment
                {
                    Id = GuidComb.Generate(),
                    ReviewId = new ReviewIdentifier(command.ProjectId, command.ReviewId),
                    CreatedAt = DateTimeOffset.UtcNow,
                    User = user,
                    Parent = parent,
                    Content = command.Content,
                    State = command.NeedsResolution ? CommentState.NeedsResolution : CommentState.NoActionNeeded,
                    Children = new List<Comment>()
                });
            }
        }
    }
}