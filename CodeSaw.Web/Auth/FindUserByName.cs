using System.Threading.Tasks;
using CodeSaw.Web.Cqrs;
using NHibernate;
using NHibernate.Linq;

namespace CodeSaw.Web.Auth
{
    public class FindUserByName : IQuery<ReviewUser>
    {
        public string UserName { get; }

        public FindUserByName(string userName)
        {
            UserName = userName;
        }

        public class Handler : IQueryHandler<FindUserByName, ReviewUser>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public Task<ReviewUser> Execute(FindUserByName query)
            {
                return _session.Query<ReviewUser>()
                    .FirstOrDefaultAsync(user => user.UserName == query.UserName);
            }
        }
    }
}