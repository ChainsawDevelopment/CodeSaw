using Nancy;

namespace Web.Modules.Api
{
    public class ReviewInfoModule : NancyModule
    {
        public ReviewInfoModule() : base("/api/review/")
        {
            Get("/{reviewId}/revisions/{previous}/{current}", _ => {
                return new {
                    changes = new [] {
                        new { path = "file1.txt" },
                        new { path = "folder1/file2.txt" },
                        new { path = "folder1/file3.txt" },
                        new { path = "folder2/file4.txt" },
                        new { path = "folder2/file5.txt" },
                    }
                };
            });
        }
    }
}