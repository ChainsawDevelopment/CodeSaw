using System;
using System.Collections.Generic;
using System.Linq;
using CodeSaw.Web.Cqrs;
using NUnit.Framework;

namespace CodeSaw.Tests.Conventions
{
    [Category("Conventions")]
    public class CommandsConventionsTest
    {
        [Test]
        [TestCaseSource(nameof(AllCommands))]
        public void NoCommandSuffix(Type commandType)
        {
            Assert.That(commandType.Name, Does.Not.EndsWith("Commands").IgnoreCase, "Command types should not be suffixed with 'Command'");
        }

        [Test]
        [TestCaseSource(nameof(AllCommands))]
        public void CommandHandlerShouldBeInnerClass(Type commandType)
        {
            var handlerType = commandType.GetNestedType("Handler");

            Assert.That(handlerType, Is.Not.Null, "Command handler should be nested class called 'Handler'");
        }

        public static IEnumerable<TestCaseData> AllCommands()
        {
            var commandTypes = typeof(ICommand).Assembly.GetTypes()
                .Where(x => x.GetInterfaces().Any(i => i == typeof(ICommand)));
                ;

            return commandTypes.Select(x => new TestCaseData(x));
        }
    }
}
