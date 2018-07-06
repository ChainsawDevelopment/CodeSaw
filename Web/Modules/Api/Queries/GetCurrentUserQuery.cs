using System.Threading.Tasks;
using Web.Auth;
using Web.Cqrs;

namespace Web.Modules.Api.Queries
{
    public class GetCurrentUserQuery : IQuery<ReviewUser>
    {
        public class Handler : IQueryHandler<GetCurrentUserQuery, ReviewUser>
        {
            private readonly ReviewUser _currentUser;

            public Handler([CurrentUser]ReviewUser currentUser) {
                _currentUser = currentUser;
            }

            public Task<ReviewUser> Execute(GetCurrentUserQuery query) {
                return Task.FromResult(_currentUser);
            }
        }
    }
}