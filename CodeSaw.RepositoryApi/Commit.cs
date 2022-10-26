using System;
using System.Collections.Generic;
using System.Text;

namespace CodeSaw.RepositoryApi
{
    public class Commit
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
    }
}
