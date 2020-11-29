
using CommandLine;

namespace CodeSaw.Archiver
{
    [Verb("run", isDefault: true, HelpText = "Archive old merge requests")]
    public class RunArchiverVerb
    {
        [Option('c', "configFile", Required = true, HelpText = "Path to configuration file used to connect to DB and GitLab")]
        public string ConfigFile { get; set; }

        [Option("projectId", Required = false, Default = null, HelpText = "Project ID for which to perform archiving. All if omitted.")]
        public int? ProjectId { get; set; }

        [Option('d', "days", Required = false, Default = 180, HelpText = "Number of days to keep the merge requests. Older than that will be archived.")]
        public int DaysThreshold { get; set; }

        [Option("batch", Required = false, Default = null, HelpText = "Size of batch to process. Process all found merge requests when omitted.")]
        public int? BatchSize { get; set; }

        [Option("deleteTags", Required = false, Default = false, HelpText = "Delete tags, by default the archiver only mark the revisions as pending, this will actually clean the tags.")]
        public bool DeleteTags { get; set; }
    };
}
