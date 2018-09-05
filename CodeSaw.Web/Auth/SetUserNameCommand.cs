using System.Threading.Tasks;
using CodeSaw.Web.Cqrs;
using NHibernate;

namespace CodeSaw.Web.Auth
{
    public class SetUserNameCommand : ICommand
    {
        public SetUserNameCommand(int userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }

        public int UserId { get; }
        public string UserName { get; }

        public class Handler : CommandHandler<SetUserNameCommand>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public override async Task Handle(SetUserNameCommand command)
            {
                await _session.UpdateAsync(new ReviewUser()
                {
                    Id = command.UserId,
                    UserName = command.UserName
                });
            }
        }
    }
}