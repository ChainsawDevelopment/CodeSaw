﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NHibernate;
using RepositoryApi;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Commands.PublishElements
{
    public class RevisionFactory
    {
        private readonly IRepository _api;
        private readonly ISession _session;

        public RevisionFactory(IRepository api, ISession session)
        {
            _api = api;
            _session = session;
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

            var previousHead = FindPreviousRevision(reviewId, number, baseCommit);

            var diff = await _api.GetDiff(reviewId.ProjectId, previousHead, headCommit);

            foreach (var file in diff)
            {
                revision.Files.Add(RevisionFile.FromDiff(file));
            }

            return revision;
        }

        private string FindPreviousRevision(ReviewIdentifier reviewId, int number, string baseCommit)
        {
            if (number <= 1)
            {
                return baseCommit;
            }

            var previousRevision = _session.Query<ReviewRevision>().Single(x => x.ReviewId == reviewId && x.RevisionNumber == number - 1);
            return previousRevision.HeadCommit;
        }
    }
}