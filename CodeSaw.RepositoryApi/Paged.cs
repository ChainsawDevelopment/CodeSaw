using System.Collections.Generic;

namespace CodeSaw.RepositoryApi
{
    public class Paged<T>
    {
        public List<T> Items { get; set; }
        public int PerPage { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }
}