using System.Collections.Generic;
using NHibernate.Criterion;

namespace CodeSaw.Web
{
    public static class QueryExtensions
    {
        public static ICriterion Disjunction(this IEnumerable<ICriterion> criteria)
        {
            var disjunction = Restrictions.Disjunction();

            foreach (var criterion in criteria)
            {
                disjunction.Add(criterion);
            }

            return disjunction;
        }
    }
}