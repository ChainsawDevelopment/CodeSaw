using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class RevisionFactory
    {
        private readonly IRepository _api;

        public RevisionFactory(IRepository api)
        {
            _api = api;
        }

        public async Task<ReviewRevision> Create(ReviewIdentifier reviewId, int number, string baseCommit, string headCommit)
        {
            var revision = new ReviewRevision
            {
                Id = GuidComb.Generate(),
                ReviewId = reviewId,
                RevisionNumber = number,
                BaseCommit = baseCommit,
                HeadCommit = headCommit
            };

            return revision;
        }
    }
}