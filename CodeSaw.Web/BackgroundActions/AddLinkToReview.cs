using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Hooks;

namespace CodeSaw.Web.BackgroundActions
{
    public class AddLinkToReview : IHandle<NewReview>, IHandle<ReviewChangedExternallyEvent>
    {
        private readonly ICommandDispatcher _commands;
        private readonly IRepository _repository;

        public AddLinkToReview(ICommandDispatcher commands, IRepository repository)
        {
            _commands = commands;
            _repository = repository;
        }

        public async Task Handle(NewReview @event)
        {
            await _commands.Execute(new RegisterReviewLink(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId));
        }

        public async Task Handle(ReviewChangedExternallyEvent @event)
        {
            var mr = await _repository.GetMergeRequestInfo(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId);
            if(mr.Description.Contains("There's a better review for that:"))
            {
                NLog.LogManager.GetLogger("AAA").Info("Link present");
                return;
            }

            await _commands.Execute(new RegisterReviewLink(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId));
        }
    }
}