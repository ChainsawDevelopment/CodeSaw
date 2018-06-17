using System;
using System.Threading.Tasks;
using NHibernate.Linq;
using Web.Cqrs;
using Web.Modules.Api.Model;
using ISession = NHibernate.ISession;

namespace Web.Modules.Api.Commands
{
    public class ResolveComment : ICommand
    {
        public int ProjectId { get; set; }
        public int ReviewId { get; set; }
        public Guid CommentId { get; set; }

        public class Handler : CommandHandler<ResolveComment>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public override async Task Handle(ResolveComment command)
            {
                var comment = await _session.Query<Comment>().FirstAsync(x => x.Id == command.CommentId);

                comment.State = CommentState.Resolved;

                await _session.UpdateAsync(comment);
            }
        }
    }
}