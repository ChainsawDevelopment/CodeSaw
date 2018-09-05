using System.Threading;
using System.Threading.Tasks;
using CodeSaw.Web.Cqrs;
using Microsoft.AspNetCore.Identity;

namespace CodeSaw.Web.Auth
{
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
            return Task.FromResult(IdentityResult.Failed(new IdentityError() { Description = "Not implemented" }));
        }

        public async Task<ReviewUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return await _queries.Query(new FindUserById(userId));
        }

        public async Task<ReviewUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return await _queries.Query(new FindUserByName(normalizedUserName.ToLowerInvariant()));
        }
    }
}