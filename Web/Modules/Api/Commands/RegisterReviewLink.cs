using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Api.Commands
{
    public class RegisterReviewLink : ICommand
    {
        public int ProjectId { get; set; }
        public int ReviewId { get; set; }
        public string BaseUrl { get; set; }

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

                await _gitlabApi.CreateNewMergeRequestNote(command.ProjectId, command.ReviewId, $"There's a better review for that: {reviewLink}");
            }
        }
    }
}