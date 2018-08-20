using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RepositoryApi;
using Web.Modules.Api.Queries;

namespace Web.Modules.Api.Model
{
    public class FileMatrix : List<FileMatrix.Entry>
    {
        private readonly RevisionId[] _revisions;

        public RevisionId LatestRevision { get; set; }

        public FileMatrix(IEnumerable<RevisionId> revisions)
        {
            _revisions = revisions.ToArray();
            LatestRevision = _revisions.Last();
        }

        public void Append(RevisionId revisionId, IEnumerable<RevisionFile> changedFiles)
        {
            foreach (var file in changedFiles)
            {
                Entry entry;
                if (file.IsRenamed)
                {
                    entry = FindRenamedEntry(revisionId, file);

                    if (entry != null)
                    {
                        entry.File = entry.File.WithNewName(file.File.NewPath);
                    }
                    else
                    {
                        entry = FindOrCreateEntry(file);
                    }
                    
                }
                else
                {
                    entry = FindOrCreateEntry(file);
                }

                entry.Revisions[revisionId] = Status.From(file);
            }
        }

        private Entry FindRenamedEntry(RevisionId revisionId, RevisionFile file)
        {
            return Find(f => f.StatusForRevision(revisionId).File.NewPath == file.File.OldPath);
        }

        private Entry FindOrCreateEntry(RevisionFile file)
        {
            var entry = Find(f => f.File.NewPath == file.File.OldPath);

            if (entry == null)
            {
                entry = CreateEmptyEntry(file);
                Add(entry);
            }

            return entry;
        }

        private Entry CreateEmptyEntry(RevisionFile file)
        {
            var entry = new Entry(_revisions) {File = file.File};

            return entry;
        }

        public class Entry
        {
            private readonly RevisionId[] _revisionsOrder;
            public PathPair File { get; set; }
            [JsonConverter(typeof(FileMatrixRevisionsConverter))]
            public SortedDictionary<RevisionId, Status> Revisions { get; set; }

            public Entry(RevisionId[] revisionsOrder)
            {
                _revisionsOrder = revisionsOrder;

                Revisions = new SortedDictionary<RevisionId, Status>(DelegateComparer.For((RevisionId r) => _revisionsOrder.IndexOf(r)));
            }

            internal Status StatusForRevision(RevisionId revision)
            {
                return Revisions.TakeWhile(x => _revisionsOrder.IndexOf(x.Key) <= _revisionsOrder.IndexOf(revision)).LastOrDefault().Value;
            }
        }

        public class Status
        {
            public PathPair File { get; set; }
            public bool IsNew { get; set; }
            public bool IsRenamed { get; set; }
            public bool IsDeleted { get; set; }
            public bool IsUnchanged { get; set; }
            public HashSet<string> Reviewers { get; } = new HashSet<string>();

            public static Status From(RevisionFile file) => new Status
            {
                File = file.File,
                IsDeleted = file.IsDeleted,
                IsNew = file.IsNew,
                IsRenamed = file.IsRenamed,
                IsUnchanged = false
            };

            public static Status Unchanged(PathPair file) => new Status
            {
                File = PathPair.Make(file.NewPath),
                IsDeleted = false,
                IsNew = false,
                IsRenamed = false,
                IsUnchanged = true
            };
        }

        public void FillUnchanged()
        {
            foreach (var file in this)
            {
                var firstRevision = file.Revisions.Keys.First();
                var firstIdx = _revisions.IndexOf(firstRevision);

                for (int revisionIdx = 0; revisionIdx < firstIdx; revisionIdx++)
                {
                    file.Revisions[_revisions[revisionIdx]] = Status.Unchanged(file.Revisions[firstRevision].File);
                }

                var previousStatus = file.Revisions[firstRevision];

                for (int revisionIdx = firstIdx + 1; revisionIdx < _revisions.Length; revisionIdx++)
                {
                    var rev = _revisions[revisionIdx];

                    if (file.Revisions.ContainsKey(rev))
                    {
                        previousStatus = file.Revisions[rev];
                        continue;
                    }

                    file.Revisions[rev] = previousStatus = Status.Unchanged(previousStatus.File);
                }
            }
        }

        public (int reviewedAtLatestRevision, int unreviewedAtLatestRevision) CalculateStatistics()
        {
            int reviewedAtLatestRevision = 0;
            int unreviewedAtLatestRevision = 0;

            foreach (var entry in this)
            {
                if (entry.Revisions.Last(x => !x.Value.IsUnchanged).Value.Reviewers.Any())
                {
                    reviewedAtLatestRevision++;
                }
                else
                {
                    unreviewedAtLatestRevision++;
                }
            }

            return (reviewedAtLatestRevision, unreviewedAtLatestRevision);
        }
    }
}