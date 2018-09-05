using System.Threading.Tasks;
using CodeSaw.Web.Cqrs;
using NHibernate;

namespace CodeSaw.Web.Auth
{
    public class UpdateUserCommand : ICommand
    {
        public ReviewUser User { get; }

        public UpdateUserCommand(ReviewUser user)
        {
            User = user;
        }

        public class Handler : CommandHandler<UpdateUserCommand>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public override async Task Handle(UpdateUserCommand command)
            {
                await _session.UpdateAsync(command.User);
            }
        }
    }
}