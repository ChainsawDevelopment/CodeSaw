using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate;
using NHibernate.Linq;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetFileMatrix : IQuery<object>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetFileMatrix(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : IQueryHandler<GetFileMatrix, object>
        {
            private readonly ISession _session;
            private readonly IRepository _api;

            public Handler(ISession session, IRepository api)
            {
                _session = session;
                _api = api;
            }

            public async Task<object> Execute(GetFileMatrix query)
            {
                var revisions = await _session.Query<ReviewRevision>()
                    .Where(x => x.ReviewId == query.ReviewId)
                    .FetchMany(x => x.Files)
                    .OrderBy(x => x.RevisionNumber)
                    .ToListAsync();

                var mergeRequest = await _api.GetMergeRequestInfo(query.ReviewId.ProjectId, query.ReviewId.ReviewId);

                var revisionIds = revisions.Select(x => (RevisionId) new RevisionId.Selected(x.RevisionNumber));

                var hasProvisional = !revisions.Any() || mergeRequest.HeadCommit != revisions.Last().HeadCommit;
                if (hasProvisional)
                {
                    revisionIds = revisionIds.Union(new RevisionId.Hash(mergeRequest.HeadCommit));
                }

                var matrix = new FileMatrix(revisionIds);

                foreach (var revision in revisions)
                {
                    matrix.Append(new RevisionId.Selected(revision.RevisionNumber), revision.Files);
                }

                if (hasProvisional)
                {
                    var provisionalDiff = await _api.GetDiff(query.ReviewId.ProjectId, revisions.LastOrDefault()?.HeadCommit ?? mergeRequest.BaseCommit, mergeRequest.HeadCommit);

                    var files = provisionalDiff.Select(RevisionFile.FromDiff);

                    matrix.Append(new RevisionId.Hash(mergeRequest.HeadCommit), files);
                }

                matrix.FillUnchanged();

                return new
                {
                    matrix
                };
            }
        }
    }

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
            var entry = Find(f => f.File == file.File);

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
    }

    public class FileMatrixRevisionsConverter: JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var revisions = (SortedDictionary<RevisionId, FileMatrix.Status>) value;

            writer.WriteStartArray();

            foreach (var (revision, status) in revisions)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("revision");
                writer.WriteStartObject();
                
                writer.WritePropertyName("type");
                writer.WriteValue(revision.Resolve(() => "base", s => "selected", h => "hash"));

                writer.WritePropertyName("value");
                writer.WriteValue(revision.Resolve(() => (object)"base", s => s.Revision, h => h.CommitHash));
                
                writer.WriteEndObject();

                var statusJson = JObject.FromObject(status, serializer);

                foreach (var property in statusJson.Properties())
                {
                    property.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType) => true;
    }
}