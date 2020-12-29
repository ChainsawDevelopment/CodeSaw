using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using CommandLine;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;
using CodeSaw.GitLab;
using CodeSaw.Web;

namespace CodeSaw.Archiver
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<RunArchiverVerb>(args)
                .WithParsed<RunArchiverVerb>(RunArchiver);
        }

        private static void RunArchiver(RunArchiverVerb options)
        {
            var config = new ConfigurationBuilder()
                            .AddJsonFile(options.ConfigFile.Trim())
                            .Build();

            var sessionFactory = BuildSessionFactory(config);
            var gitLab = BuildGitLabApi(config);

            Console.WriteLine(options.ToString());

            MarkAsArchivePending(options, sessionFactory, gitLab);
            ArchivePendingRevisions(options, sessionFactory, gitLab);
        }

        /// <summary>
        ///   Marks old closed and merged merge requests (determine by options DaysThreshold parameter) as "archive pending".
        ///   This method does not update Git in any way.
        /// </summary>
        private static void MarkAsArchivePending(RunArchiverVerb options, ISessionFactory sessionFactory, GitLabApi gitLab)
        {
            Console.WriteLine("Marking As ArchivePending");
            using (var session = sessionFactory.OpenSession())
            {
                var mergeRequests = FindPotentialMergeRequests(options, session);

                int counter = 0;

                var toArchive = WhereEligibleToArchiving(mergeRequests, options, gitLab, id => Console.WriteLine($"[{counter++}/{mergeRequests.Length}] Ignoring {id.ProjectId}/{id.ReviewId}"));

                foreach (var mrId in toArchive)
                {
                    counter++;
                    var sqlUpdate = "UPDATE [Revisions] SET [ArchiveState] = :archivePending WHERE ProjectId = :projectId and ReviewId = :reviewId";

                    Console.WriteLine($"[{counter}/{mergeRequests.Length}] Setting ArchivePending for {mrId.ProjectId}/{mrId.Id} merged/closed at: {mrId.MergedAt ?? mrId.ClosedAt}");

                    session.CreateSQLQuery(sqlUpdate)
                        .SetParameter("archivePending", (int)ArchiveState.ArchivePending)
                        .SetParameter("projectId", mrId.ProjectId)
                        .SetParameter("reviewId", mrId.Id)
                    .ExecuteUpdate();
                }
            }
        }

        /// <summary>
        ///     Archives all revisions that are marked as "archive pending". If options DeleteTags is set this will remove tags from GitLag.
        ///     If Deleting is successfull (deleted or didn't exist) then the revision is marked as "Archived".
        /// </summary>
        private static void ArchivePendingRevisions(RunArchiverVerb options, ISessionFactory sessionFactory, GitLabApi gitLab)
        {
            Console.WriteLine("Archiving Pending Revisions");
            var tagNameFormat = "reviewer/{0}/r{1}/{2}";

            using (var session = sessionFactory.OpenSession())
            {
                var pendingRevisions = session.Query<ReviewRevision>().Where(x => x.ArchiveState == (int)ArchiveState.ArchivePending).ToArray();
                int counter = 0;

                foreach (var revision in pendingRevisions)
                {
                    counter++;
                    Console.WriteLine($"[{counter}/{pendingRevisions.Length}] Clearing Tags for {revision.ReviewId.ProjectId}/{revision.ReviewId.ReviewId}");

                    var baseTag = string.Format(tagNameFormat, revision.ReviewId.ReviewId, revision.RevisionNumber, "base");
                    var headTag = string.Format(tagNameFormat, revision.ReviewId.ReviewId, revision.RevisionNumber, "head");

                    bool success = true;

                    success = DeleteTag(options, revision, baseTag, gitLab) && success;
                    success = DeleteTag(options, revision, headTag, gitLab) && success;

                    if (success && options.DeleteTags)
                    {
                        revision.ArchiveState = (int)ArchiveState.Archived;
                        session.Save(revision);
                        
                        session.Flush();
                    }
                }

                session.Flush();
            }
        }

        private static IEnumerable<MergeRequest> WhereEligibleToArchiving(
            IEnumerable<ReviewIdentifier> mergeRequests,
            RunArchiverVerb options,
            GitLabApi gitLab,
            Action<ReviewIdentifier> notEligibleCallback)
        {
            foreach (var mergeRequest in mergeRequests)
            {
                MergeRequest mrInfo = null;
                try
                {
                    mrInfo = gitLab.GetMergeRequestInfo(mergeRequest.ProjectId, mergeRequest.ReviewId).Result;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                if (mrInfo != null && (mrInfo.State == MergeRequestState.closed || mrInfo.State == MergeRequestState.merged))
                {
                    var finalDate = mrInfo.MergedAt ?? mrInfo.ClosedAt;
                    if (!finalDate.HasValue)
                    {
                        notEligibleCallback(mergeRequest);
                        continue;
                    }

                    if (finalDate.Value < options.ThresholdDate)
                    {
                        yield return mrInfo;
                    }
                    else
                    {
                        notEligibleCallback(mergeRequest);
                    }
                }
                else
                {
                    notEligibleCallback(mergeRequest);
                }
            }
        }

        private static ReviewIdentifier[] FindPotentialMergeRequests(RunArchiverVerb options, ISession session)
        {
            var thresholdDate = options.ThresholdDate;

            ReviewRevision revision = null;
            Review review = null;

            var reviewsQuery = session.QueryOver<ReviewRevision>(() => revision).Where(() => revision.ArchiveState == (int)ArchiveState.NotArchived)
                .JoinEntityAlias(() => review, () => revision.Id == review.RevisionId)
                .Where(() => review.ReviewedAt < thresholdDate);

            if (options.ProjectId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(() => revision.ReviewId.ProjectId == options.ProjectId.Value);
            }


            IEnumerable<ReviewIdentifier> idList = reviewsQuery
                .Select(Projections.Distinct(Projections.Property(() => revision.ReviewId)))
                .List<ReviewIdentifier>();


            if (options.BatchSize.HasValue)
            {
                idList = idList.Take(options.BatchSize.Value);
            }

            var mergeRequests = idList.ToArray();

            Console.WriteLine($"Total of {mergeRequests.Length} potential archive merge requests.");

            return mergeRequests;
        }

        private static bool DeleteTag(RunArchiverVerb options, ReviewRevision revision, string tagName, GitLabApi gitLab)
        {
            if (options.DeleteTags)
            {
                try
                {
                    Console.WriteLine($"Deleting {tagName} from project {revision.ReviewId.ProjectId}");
                    gitLab.DeleteRef(revision.ReviewId.ProjectId, tagName).Wait();
                }
                catch (System.AggregateException)
                {
                    Console.WriteLine($"Failed to delete {tagName}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"Would delete {tagName} from project {revision.ReviewId.ProjectId}");
            }

            return true;
        }

        static private ISessionFactory BuildSessionFactory(IConfiguration programConfig)
        {
            var configuration = new NHibernate.Cfg.Configuration();

            configuration.SetProperty(NHibernate.Cfg.Environment.ConnectionString, programConfig.GetConnectionString("Store"));
            configuration.SetProperty(NHibernate.Cfg.Environment.Dialect, typeof(MsSql2012Dialect).AssemblyQualifiedName);

            var modelMapper = new ModelMapper();
            modelMapper.AddMappings(typeof(CodeSaw.Web.Startup).Assembly.GetExportedTypes());

            var hbm = modelMapper.CompileMappingForAllExplicitlyAddedEntities();

            configuration.AddMapping(hbm);

            return configuration.BuildSessionFactory();
        }

        private static GitLabApi BuildGitLabApi(IConfiguration configuration)
        {
            var cfg = configuration.GetSection("GitLab");
            var globalToken = cfg.GetValue<string>("globalToken");
            var accessTokenSource = new CustomToken(globalToken);
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            bool readOnly = false;
            if (!bool.TryParse(cfg["readOnly"] ?? "", out readOnly))
            {
                readOnly = false;
            }

            return GitLabApiFactory.CreateGitLabApi(cfg["url"], accessTokenSource, cfg["Proxy"], memoryCache, readOnly);
        }
    }
}
