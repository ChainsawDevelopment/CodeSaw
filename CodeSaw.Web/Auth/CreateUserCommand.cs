using System.Threading.Tasks;
using CodeSaw.Web.Cqrs;
using NHibernate;

namespace CodeSaw.Web.Auth
{
    public class CreateUserCommand : ICommand
    {
        public ReviewUser User { get; }

        public CreateUserCommand(ReviewUser user)
        {
            User = user;
        }

        public class Handler : CommandHandler<CreateUserCommand>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public override async Task Handle(CreateUserCommand command)
            {
                await _session.SaveAsync(command.User);
            }
        }
    }
}