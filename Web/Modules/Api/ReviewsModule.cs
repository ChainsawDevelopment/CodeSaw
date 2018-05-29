using Nancy;

namespace Web.Modules.Api
{
    public class ReviewsModule : NancyModule
    {
        public ReviewsModule() : base("/api/reviews")
        {
            Get("/", _ =>
            {
                return new[]
                {
                    new {id = 12, title = "Mix human and protomolecule", changedFiles = 30},
                    new {id = 21, title = "Recover Tachi", changedFiles = 2},
                    new {id = 123, title = "Inspect Venus", changedFiles = 45},
                    new {id = 321, title = "Buy remaining books", changedFiles = 12},
                    new {id = 322, title = "It reaches out...", changedFiles = 12},
                    new {id = 323, title = "Hit Eros with Navoo so it will not collide with Earth", changedFiles = 12},
                };
            });
        }
    }
}