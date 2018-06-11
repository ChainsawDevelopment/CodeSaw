using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Api.Commands
{
    public class RegisterReviewLink : ICommand
    {
        public RegisterReviewLink(int projectId, int reviewId, string baseUrl)
        {
            ProjectId = projectId;
            ReviewId = reviewId;
            BaseUrl = baseUrl;
        }

        public int ProjectId { get; }
        public int ReviewId { get; }
        public string BaseUrl { get; }

        public class Handler : CommandHandler<RegisterReviewLink>
        {
            private readonly IRepository _gitlabApi;

            public Handler(IRepository gitlabApi)
            {
                _gitlabApi = gitlabApi;
            }

            public override async Task Handle(RegisterReviewLink command)
            {
                string reviewLink = $"{command.BaseUrl}/project/{command.ProjectId}/review/{command.ReviewId}";
                
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