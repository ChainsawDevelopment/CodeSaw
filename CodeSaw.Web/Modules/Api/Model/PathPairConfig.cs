using CodeSaw.RepositoryApi;
using NHibernate.Mapping.ByCode.Conformist;

namespace CodeSaw.Web.Modules.Api.Model
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