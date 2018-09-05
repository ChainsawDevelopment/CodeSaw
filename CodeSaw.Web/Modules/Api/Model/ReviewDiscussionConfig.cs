using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace Web.Modules.Api.Model
{
    public class ReviewDiscussionConfig : UnionSubclassMapping<ReviewDiscussion>
    {
        public ReviewDiscussionConfig()
        {
            Table("ReviewDiscussions");
        }
    }
}