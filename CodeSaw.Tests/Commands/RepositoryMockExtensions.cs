using System.Linq;
using CodeSaw.RepositoryApi;
using Moq;

namespace CodeSaw.Tests.Commands
{
    public static class RepositoryMockExtensions
    {
        public static void SetupDiff(this Mock<IRepository> @this, string @base, string head, params FileDiff[] diff)
        {
            @this.Setup(x => x.GetDiff(It.IsAny<int>(), @base, head)).ReturnsAsync(diff.ToList);
        }
    }
}