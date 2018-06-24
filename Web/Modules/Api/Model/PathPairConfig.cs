using NHibernate.Mapping.ByCode.Conformist;
using RepositoryApi;

namespace Web.Modules.Api.Model
{
    public class PathPairConfig : ComponentMapping<PathPair>
    {
        public PathPairConfig()
        {
            Property(x=>x.OldPath);
            Property(x=>x.NewPath);
        }
    }
}