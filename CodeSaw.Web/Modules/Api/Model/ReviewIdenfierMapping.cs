using CodeSaw.RepositoryApi;
using NHibernate.Mapping.ByCode.Conformist;

namespace CodeSaw.Web.Modules.Api.Model
{
    public class ReviewIdenfierMapping : ComponentMapping<ReviewIdentifier>
    {
        public ReviewIdenfierMapping()
        {
            Property(x => x.ProjectId);
            Property(x => x.ReviewId);
        }
    }
}