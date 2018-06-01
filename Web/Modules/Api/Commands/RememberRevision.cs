using System.Threading.Tasks;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Mapping.ByCode;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Commands
{
    public class RememberRevision : ICommand
    {
        public int ProjectId { get; set; }
        public int ReviewId { get; set; }
        public string HeadCommit { get; set; }
        public string BaseCommit { get; set; }

        public class Handler : CommandHandler<RememberRevision>
        {
            private readonly IRepository _api;
            private readonly ISession _session;

            public Handler(IRepository api, ISession session)
            {
                _api = api;
                _session = session;
            }

            public override async Task Handle(RememberRevision command)
            {
                int revision = 1 + (_session.QueryOver<ReviewRevision>()
                                        .Where(x => x.ReviewId.ProjectId == command.ProjectId && x.ReviewId.ReviewId == command.ReviewId)
                                        .Select(Projections.Max<ReviewRevision>(x => x.RevisionNumber))
                                        .SingleOrDefault<int?>() ?? 0);

                await _api.CreateRef(command.ProjectId, name: $"reviewer/{command.ReviewId}/r{revision}/base", commit: command.BaseCommit);
                await _api.CreateRef(command.ProjectId, name: $"reviewer/{command.ReviewId}/r{revision}/head", commit: command.HeadCommit);

                await _session.SaveAsync(new ReviewRevision
                {
                    Id = GuidComb.Generate(),
                    ReviewId = new ReviewIdentifier(command.ProjectId, command.ReviewId),
                    RevisionNumber = revision,
                    BaseCommit = command.BaseCommit,
                    HeadCommit = command.HeadCommit
                });
            }
        }
    }
}