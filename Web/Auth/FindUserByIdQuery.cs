using System.Threading.Tasks;
using NHibernate;
using Web.Cqrs;

namespace Web.Auth
{
    public class FindUserByIdQuery : IQuery<ReviewUser>
    {
        public string UserId { get; }

        public FindUserByIdQuery(string userId)
        {
            UserId = userId;
        }

        public class Handler : IQueryHandler<FindUserByIdQuery, ReviewUser>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public Task<ReviewUser> Execute(FindUserByIdQuery query)
            {
                return _session.GetAsync<ReviewUser>(query.UserId);
            }
        }
    }
}