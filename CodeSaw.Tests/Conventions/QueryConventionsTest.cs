using System;
using System.Collections.Generic;
using System.Linq;
using CodeSaw.Web.Cqrs;
using NUnit.Framework;

namespace CodeSaw.Tests.Conventions
{
    [Category("Conventions")]
    public class QueryConventionsTest
    {
        [Test]
        [TestCaseSource(nameof(AllQueries))]
        public void NoQuerySuffix(Type queryType)
        {
            Assert.That(queryType.Name, Does.Not.EndsWith("Query").IgnoreCase, "Query types should not be suffixed with 'Query'");
        }

        [Test]
        [TestCaseSource(nameof(AllQueries))]
        public void QueryHandlerShouldBeInnerClass(Type queryType)
        {
            var handlerType = queryType.GetNestedType("Handler");

            Assert.That(handlerType, Is.Not.Null, "Query handler should be nested class called 'Handler'");
        }

        public static IEnumerable<TestCaseData> AllQueries()
        {
            var queryTypes = typeof(IQuery<>).Assembly.GetTypes()
                    .Where(x => x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)));

            return queryTypes.Select(x => new TestCaseData(x));
        }
    }
}
