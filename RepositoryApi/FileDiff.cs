namespace RepositoryApi
{
    public class FileDiff
    {
        public string OldPath { get; set; }
        public string NewPath { get; set; }
        public bool NewFile { get; set; }
        public bool RenamedFile { get; set; }
        public bool DeletedFile { get; set; }
    }
}