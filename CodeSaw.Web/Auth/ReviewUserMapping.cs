using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace CodeSaw.Web.Auth
{
    public class ReviewUserMapping : ClassMapping<ReviewUser>
    {
        public ReviewUserMapping()
        {
            Table("Users");
            Id(x => x.Id, id => id.Generator(Generators.Identity));
            Property(x => x.UserName);
            Property(x => x.Token);
            Property(x => x.Name);
            Property(x => x.AvatarUrl);
        }
    }
}