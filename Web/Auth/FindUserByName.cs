using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using Web.Cqrs;

namespace Web.Auth
{
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
}