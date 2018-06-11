using System.Threading.Tasks;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Mapping.ByCode;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Commands
{
    public class MergePullRequest : ICommand
    {
        public int ProjectId { get; set; }
        public int ReviewId { get; set; }
        public bool ShouldRemoveBranch { get; set; }
        public string CommitMessage { get; set; }

        public class Handler : CommandHandler<MergePullRequest>
        {
            private readonly IRepository _api;
            private readonly ISession _session;

            public Handler(IRepository api, ISession session)
            {
                _api = api;
                _session = session;
            }

            public override async Task Handle(MergePullRequest command)
            {
                await _api.MergePullRequest(command.ProjectId, command.ReviewId, command.ShouldRemoveBranch,
                    command.CommitMessage);
            }
        }
    }
}