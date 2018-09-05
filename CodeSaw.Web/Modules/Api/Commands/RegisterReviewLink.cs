using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;

namespace CodeSaw.Web.Modules.Api.Commands
{
    public class RegisterReviewLink : ICommand
    {
        public RegisterReviewLink(int projectId, int reviewId)
        {
            ProjectId = projectId;
            ReviewId = reviewId;
        }

        public int ProjectId { get; }
        public int ReviewId { get; }

        public class Handler : CommandHandler<RegisterReviewLink>
        {
            private readonly IRepository _gitlabApi;
            private readonly string _siteBase;

            public Handler(IRepository gitlabApi, [SiteBase]string siteBase)
            {
                _gitlabApi = gitlabApi;
                _siteBase = siteBase;
            }

            public override async Task Handle(RegisterReviewLink command)
            {
                string reviewLink = $"{_siteBase}/project/{command.ProjectId}/review/{command.ReviewId}";
                
                await _gitlabApi.UpdateDescription(new MergeRequest()
                {
                    Id = command.ReviewId,
                    ProjectId = command.ProjectId,
                    Description = $"There's a better review for that: {reviewLink}"
                });
            }
        }
    }
}