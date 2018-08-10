using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Moq.Language.Flow;
using Web.Cqrs;

namespace Tests.Api
{
    public static class QueryMockExtensions
    {
        public static QuerySetup<TQuery, TResult> ForQuery<TQuery, TResult>(this Mock<IQueryRunner> mock, Expression<Func<TQuery, bool>> predicate = null) 
            where TQuery : IQuery<TResult>
        {
            return new QuerySetup<TQuery, TResult>(mock, predicate ?? (x => true));
        }

        public class QuerySetup<TQuery, TResult> 
            where TQuery: IQuery<TResult>
        {
            private readonly ISetup<IQueryRunner, Task<TResult>> _setup;

            public QuerySetup(Mock<IQueryRunner> mock, Expression<Func<TQuery, bool>> predicate)
            {
                _setup = mock.Setup(x => x.Query(It.Is<TQuery>(predicate)));
            }

            public void Returns(TResult value)
            {
                _setup.Returns(Task.FromResult(value));
            }
        }
    }
}