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

        public Task<ReviewUser> Execute(ISession session)
        {
            return session.GetAsync<ReviewUser>(UserId);
        }
    }
}