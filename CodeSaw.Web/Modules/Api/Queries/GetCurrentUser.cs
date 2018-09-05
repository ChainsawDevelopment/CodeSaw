using System.Threading.Tasks;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class GetCurrentUser : IQuery<ReviewUser>
    {
        public class Handler : IQueryHandler<GetCurrentUser, ReviewUser>
        {
            private readonly ReviewUser _currentUser;

            public Handler([CurrentUser]ReviewUser currentUser) {
                _currentUser = currentUser;
            }

            public Task<ReviewUser> Execute(GetCurrentUser query) {
                return Task.FromResult(_currentUser);
            }
        }
    }
}