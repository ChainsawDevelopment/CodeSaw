namespace RepositoryApi
{
    public class PathPair
    {
        public string OldPath { get; set; }
        public string NewPath { get; set; }
    }

    public class FileDiff
    {
        public PathPair Path { get; set; } = new PathPair();
        public bool NewFile { get; set; }
        public bool RenamedFile { get; set; }
        public bool DeletedFile { get; set; }
    }
}