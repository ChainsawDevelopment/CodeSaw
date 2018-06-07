using System;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Criterion;
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
                if (DoesRevisionExist(command))
                {
                    // If it's already there, then we don't need to add anything.
                    return;
                }
                
                int revisionNumber = GetNextRevisionNumber(command);

                // If this throws an exception then we may have concurency issue
                await CreateRef(command, revisionNumber, command.BaseCommit, "base");
                
                try
                {
                    await CreateRef(command, revisionNumber, command.HeadCommit, "head");
                }
                catch (Exception unexpectedException)
                {
                    // The base ref is already created, we must add the record to database no matter what
                    Console.WriteLine("Failed to create ref for head commit - ignoring");
                    Console.WriteLine(unexpectedException.ToString());
                }

                await _session.SaveAsync(new ReviewRevision
                {
                    Id = GuidComb.Generate(),
                    ReviewId = new ReviewIdentifier(command.ProjectId, command.ReviewId),
                    RevisionNumber = revisionNumber,
                    BaseCommit = command.BaseCommit,
                    HeadCommit = command.HeadCommit
                });
            }

            private async Task CreateRef(RememberRevision command, int revision, string commitRef, string refType)
            {
                await _api.CreateRef(
                    projectId: command.ProjectId,
                    name: $"reviewer/{command.ReviewId}/r{revision}/{refType}", 
                    commit: commitRef);
            }

            private int GetNextRevisionNumber(RememberRevision command)
            {
                return 1 + (_session.QueryOver<ReviewRevision>()
                                .Where(x => x.ReviewId.ProjectId == command.ProjectId && x.ReviewId.ReviewId == command.ReviewId)
                                .Select(Projections.Max<ReviewRevision>(x => x.RevisionNumber))
                                .SingleOrDefault<int?>() ?? 0);
            }

            private bool DoesRevisionExist(RememberRevision command)
            {
                return _session.QueryOver<ReviewRevision>()
                                 .Where(x => x.ReviewId.ProjectId == command.ProjectId &&
                                             x.ReviewId.ReviewId == command.ReviewId)
                                 .Where(x => x.BaseCommit == command.BaseCommit && x.HeadCommit == command.HeadCommit)
                                 .Select(x => x.Id)
                                 .RowCount() > 0;
            }
        }
    }
}