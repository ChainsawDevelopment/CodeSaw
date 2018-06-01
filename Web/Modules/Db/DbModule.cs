using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Nancy;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Web.Cqrs;

namespace Web.Modules.Db
{
    public class DbModule : NancyModule
    {
        public DbModule(ICommandDispatcher commands, IQueryRunner queries) : base("/test/db")
        {
            Get("/insert/{text}", async _ =>
            {
                await commands.Execute(new InsertRecord(_.Text));
                return new {ok = true};
            });
            Get("/list", async _ => new
            {
                list = await queries.Query(new ListEntities())
            });
        }
    }

    public class InsertRecord : ICommand
    {
        public InsertRecord(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public class Handler : CommandHandler<InsertRecord>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public override async Task Handle(InsertRecord command)
            {
                await _session.SaveAsync(new TestEntity()
                {
                    Text = command.Text
                });
            }
        }
    }

    public class ListEntities : IQuery<IList<string>>
    {
        public async Task<IList<string>> Execute(ISession session)
        {
            return await session.QueryOver<TestEntity>()
                .Select(t => t.Text)
                .ListAsync<string>();
        }
    }

    public class TestEntity
    {
        public virtual int Id { get; set; }
        public virtual string Text { get; set; }
    }

    public class TestEntityMapping : ClassMapping<TestEntity>
    {
        public TestEntityMapping()
        {
            Table("Test");
            Id(x => x.Id, id => id.Generator(Generators.Identity));
            Property(x => x.Text);
        }
    }

    public class ReviewUser
    {
        public virtual int Id { get; set; }

        public virtual string UserName { get; set; }

        public virtual string Token { get; set; }
    }

    public class ReviewUserMapping : ClassMapping<ReviewUser>
    {
        public ReviewUserMapping()
        {
            Table("Users");
            Id(x => x.Id, id => id.Generator(Generators.Identity));
            Property(x => x.UserName);
            Property(x => x.Token);
        }
    }

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

    public class NHibernateUserStore : IUserStore<ReviewUser>
    {
        private ICommandDispatcher _commands;
        private IQueryRunner _queries;

        public NHibernateUserStore(ICommandDispatcher commands, IQueryRunner queries)
        {
            _commands = commands;
            _queries = queries;
        }

        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(ReviewUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id.ToString());
        }

        public Task<string> GetUserNameAsync(ReviewUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName.ToLowerInvariant());
        }

        public async Task SetUserNameAsync(ReviewUser user, string userName, CancellationToken cancellationToken)
        {
            await _commands.Execute(new SetUserNameCommand(user.Id, user.UserName.ToLowerInvariant()));
        }

        public Task<string> GetNormalizedUserNameAsync(ReviewUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName.ToLowerInvariant());
        }

        public Task SetNormalizedUserNameAsync(ReviewUser user, string normalizedName, CancellationToken cancellationToken)
        {
            user.UserName = normalizedName.ToLowerInvariant();
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> CreateAsync(ReviewUser user, CancellationToken cancellationToken)
        {
            await _commands.Execute(new CreateUserCommand(user));
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(ReviewUser user, CancellationToken cancellationToken)
        {
            await _commands.Execute(new UpdateUserCommand(user));
            return IdentityResult.Success;
        }

        public Task<IdentityResult> DeleteAsync(ReviewUser user, CancellationToken cancellationToken)
        {
            // TODO: Implement
            return Task.FromResult(IdentityResult.Failed(new IdentityError() { Description = "Not implemented"}));
        }

        public async Task<ReviewUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return await _queries.Query(new FindUserByIdQuery(userId));
        }

        public async Task<ReviewUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return await _queries.Query(new FindUserByName(normalizedUserName.ToLowerInvariant()));
        }
    }

    public class FindUserByName : IQuery<ReviewUser>
    {
        public string UserName { get; }

        public FindUserByName(string userName)
        {
            UserName = userName;
        }

        public Task<ReviewUser> Execute(ISession session)
        {
            return session.Query<ReviewUser>()
                .FirstOrDefaultAsync(user => user.UserName == UserName);
        }
    }

    public class FindUserByIdQuery : IQuery<ReviewUser>
    {
        public string UserId { get; }

        public FindUserByIdQuery(string userId)
        {
            UserId = userId;
        }

        public Task<ReviewUser> Execute(ISession session)
        {
            return session.GetAsync<ReviewUser>(UserId);
        }
    }

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