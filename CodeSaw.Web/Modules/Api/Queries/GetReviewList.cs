using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;
using NHibernate.Criterion;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class GetReviewList : IQuery<Paged<GetReviewList.Item>>
    {
        public MergeRequestSearchArgs Args { get; }

        public GetReviewList(MergeRequestSearchArgs args)
        {
            Args = args;
        }

        public class Item
        {
            public UserInfo Author { get; set; }
            public ReviewIdentifier ReviewId { get; set; }
            public string Title { get; set; }
            public string Project { get; set; }
            public string WebUrl { get; set; }
            public string SourceBranch { get; set; }
            public string TargetBranch { get; set; }
            public bool IsCreatedByMe { get; set; }
            public bool AmIReviewer { get; set; }
        }

        public class Handler : IQueryHandler<GetReviewList, Paged<Item>>
        {
            private readonly IRepository _repository;
            private readonly ReviewUser _currentUser;
            private readonly ISession _session;

            public Handler(IRepository repository, [CurrentUser]ReviewUser currentUser, ISession session)
            {
                _repository = repository;
                _currentUser = currentUser;
                _session = session;
            }

            public async Task<Paged<Item>> Execute(GetReviewList query)
            {
                var activeMergeRequest = await _repository.MergeRequests(query.Args);

                var projects = await Task.WhenAll(activeMergeRequest.Items.Select(x => x.ProjectId).Distinct().Select(async x => await _repository.Project(x)));

                var reviewIds = activeMergeRequest.Items.Select(x => new ReviewIdentifier(x.ProjectId, x.Id)).ToList();

                ReviewRevision revision = null;

                var reviewIdCriterion = reviewIds.Select(id => Restrictions.And(
                    Restrictions.Eq(Projections.Property(() => revision.ReviewId.ProjectId), id.ProjectId),
                    Restrictions.Eq(Projections.Property(() => revision.ReviewId.ReviewId), id.ReviewId)
                )).Disjunction();

                Review review = null;
                var reviewerFor = _session.QueryOver<ReviewRevision>(() => revision)
                    .Where(reviewIdCriterion)
                    .JoinEntityAlias(() => review, () => revision.Id == review.RevisionId)
                    .Where(() => review.UserId == _currentUser.Id)
                    .Select(Projections.Distinct(Projections.Property(() => revision.ReviewId)))
                    .List<ReviewIdentifier>();
                    

                // todo: extend with reviewer-specific information

                var items = (from mr in activeMergeRequest.Items
                        join project in projects on mr.ProjectId equals project.Id
                        let reviewId = new ReviewIdentifier(mr.ProjectId, mr.Id)
                        select new Item
                        {
                            ReviewId = reviewId,
                            Author = mr.Author,
                            Title = mr.Title,
                            Project = $"{project.Namespace}/{project.Name}",
                            WebUrl = mr.WebUrl,
                            SourceBranch = mr.SourceBranch,
                            TargetBranch = mr.TargetBranch,
                            IsCreatedByMe = mr.Author.Username == _currentUser.UserName,
                            AmIReviewer = reviewerFor.Contains(reviewId)
                        }
                    ).ToList();

                return new Paged<Item>
                {
                    Items = items,
                    TotalItems = activeMergeRequest.TotalItems,
                    TotalPages = activeMergeRequest.TotalPages,
                    PerPage = activeMergeRequest.PerPage,
                    Page = activeMergeRequest.Page
                };
            }
        }
    }
}