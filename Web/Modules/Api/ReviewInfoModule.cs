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

            //TODO: to diff module
            Get("/{reviewId}/diff/{previous}/{current}/{file*}", _ => {
                return new {
                    info = new {
                        reviewId = (int)_.reviewId,
                        previous = (int)_.previous,
                        current = (int)_.current,
                        path = (string)_.file
                    },
                    chunks = new [] {
                        new {
                            classification = "unchanged",
                            operation = "equal",
                            text = "block1\n"
                        },
                        new {
                            classification = "base",
                            operation = "insert",
                            text = "line1.1\nline1.2\n"
                        },
                        new {
                            classification = "unchanged",
                            operation = "equal",
                            text = "\nblock2\nline2.1\nline2.2\n"
                        },
                        new {
                            classification = "review",
                            operation = "insert",
                            text = "\nblock3\nline3.1\nline3.2\n"
                        }
                    }
                };
            });
        }
    }
}