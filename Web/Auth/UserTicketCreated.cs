using System;
using System.Threading.Tasks;
using NHibernate;
using Web.Cqrs;

namespace Web.Auth
{
    public class UserTicketCreated : ICommand
    {
        public string UserName { get; }
        public string AccessToken { get; }

        public UserTicketCreated(string userName, string accessToken)
        {
            UserName = userName;
            AccessToken = accessToken;
        }

        public class Handler : CommandHandler<UserTicketCreated>
        {
            private readonly ISession _session;
            private readonly IQueryRunner _queryRunner;
            

            public Handler(ISession session, IQueryRunner queryRunner)
            {
                _session = session;
                _queryRunner = queryRunner;
            }

            public override async Task Handle(UserTicketCreated command)
            {
                var existingUser = await _queryRunner.Query(new FindUserByName(command.UserName));
                if (existingUser == null)
                {
                    await _session.SaveAsync(new ReviewUser() { UserName = command.UserName, Token = command.AccessToken });
                    Console.WriteLine("New user created");
                }
                else
                {
                    existingUser.Token = command.AccessToken;
                    await _session.UpdateAsync(existingUser);
                    Console.WriteLine($"Found existing user with ID {existingUser.Id}, token updated.");
                }
            }
        }
    }
}