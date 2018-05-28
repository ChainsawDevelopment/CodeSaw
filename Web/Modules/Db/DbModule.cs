using Nancy;
using NHibernate;

namespace Web.Modules.Db
{
    public class DbModule : NancyModule
    {
        public DbModule(ISessionFactory sf) : base("/test/db")
        {
            Get("/a", _ =>
            {
                using (var s = sf.OpenSession())
                {
                    return new
                    {
                        test = 1,
                        db_count = s.CreateSQLQuery("select count(*) from sys.databases").UniqueResult<int>()
                    };
                    
                }
            });
        }
    }
}