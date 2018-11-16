using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_10_28_11_21)]
    public class Migration_2018_10_28_11_21_CreateRevisionFileHistory : Migration
    {
        public override void Up()
        {
            Create.Table("FileHistory").InSchema("dbo")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
                .WithColumn("FileId").AsGuid().NotNullable()
                .WithColumn("RevisionId").AsGuid().ForeignKey("Revisions", "Id").Nullable()
                .WithColumn("ProjectId").AsInt32().NotNullable()
                .WithColumn("ReviewId").AsInt32().NotNullable()
                .WithColumn("FileName").AsMaxString()
                .WithColumn("IsNew").AsBoolean().NotNullable()
                .WithColumn("IsRenamed").AsBoolean().NotNullable()
                .WithColumn("IsDeleted").AsBoolean().NotNullable()
                .WithColumn("IsModified").AsBoolean().NotNullable()
                ;

            Execute.WithConnection(FillFileHistory);
        }

        private void FillFileHistory(IDbConnection conn, IDbTransaction transaction)
        {
            var revisions = transaction
                .ExecuteQuery("select rev.ProjectId, rev.ReviewId, rev.Id, rev.RevisionNumber from dbo.Revisions rev")
                .ToList(x => new {ProjectId = (int) x["ProjectId"], ReviewId = (int) x["ReviewId"], RevisionId = (Guid) x["Id"], RevisionNumber = (int) x["RevisionNumber"]})
                .GroupBy(x => new {x.ProjectId, x.ReviewId});

            foreach (var revision in revisions)
            {
                FillHistoryForSingleReview(transaction, revision.Key.ProjectId, revision.Key.ReviewId, revision.OrderBy(x => x.RevisionNumber).Select(x => x.RevisionId).ToArray());
            }
        }

        private void FillHistoryForSingleReview(IDbTransaction transaction, int projectId, int reviewId, Guid[] revisions)
        {
            var fileToId = new Dictionary<string, Guid>();

            var insertCmd = transaction.CreateCommand("insert into dbo.FileHistory(Id, ProjectId, ReviewId, FileId, RevisionId, FileName, IsNew, IsRenamed, IsDeleted, IsModified) values(NEWID(), @ProjectId, @ReviewId, @FileId, @RevisionId, @FileName, @N, @R, @D, @M)");

            insertCmd.CreateParameter("@ProjectId", DbType.Int32, projectId);
            insertCmd.CreateParameter("@ReviewId", DbType.Int32, reviewId);

            insertCmd.CreateParameter("@FileId", DbType.Guid);
            insertCmd.CreateParameter("@RevisionId", DbType.Guid);
            insertCmd.CreateParameter("@FileName", DbType.String);
            insertCmd.CreateParameter("@N", DbType.Boolean);
            insertCmd.CreateParameter("@R", DbType.Boolean);
            insertCmd.CreateParameter("@D", DbType.Boolean);
            insertCmd.CreateParameter("@M", DbType.Boolean);

            void InsertOne(Guid? revision, Guid id, string fileName, bool isNew, bool isRenamed, bool isDeleted, bool isModified)
            {
                ((DbParameter) insertCmd.Parameters["@FileId"]).Value = id;
                ((DbParameter) insertCmd.Parameters["@FileName"]).Value = fileName;
                ((DbParameter) insertCmd.Parameters["@RevisionId"]).Value = (object)revision ?? DBNull.Value;
                ((DbParameter) insertCmd.Parameters["@N"]).Value = isNew;
                ((DbParameter) insertCmd.Parameters["@R"]).Value = isRenamed;
                ((DbParameter) insertCmd.Parameters["@D"]).Value = isDeleted;
                ((DbParameter) insertCmd.Parameters["@M"]).Value = isModified;
                

                insertCmd.ExecuteNonQuery();
            }

            Guid? previousRevision = null;

            foreach (var revision in revisions)
            {
                var filesChangedInRevision = FilesChangedInRevision(transaction, revision);
                var entries = new Dictionary<Guid, FileEntry>();

                foreach (var entry in filesChangedInRevision)
                {
                    Guid fileId;
                    if (!fileToId.TryGetValue(entry.OldPath, out fileId))
                    {
                        // new file
                        fileId = Guid.NewGuid();
                    }

                    if (entry.OldPath != entry.NewPath && !fileToId.ContainsKey(entry.OldPath))
                    {
                        InsertOne(previousRevision, fileId, entry.OldPath, false, false, false, false);
                    }

                    fileToId.Remove(entry.OldPath);
                    fileToId[entry.NewPath] = fileId;
                    entries[fileId] = entry;
                }

                foreach (var (fileName, id) in fileToId)
                {
                    var entry = entries.GetValueOrDefault(id);
                    InsertOne(revision, id, fileName, entry?.IsNew ?? false, entry?.IsRenamed ?? false, entry?.IsDeleted ?? false, entry != null);
                }

                previousRevision = revision;
            }
        }

        List<FileEntry> FilesChangedInRevision(IDbTransaction transaction, Guid revisionId)
        {
            return transaction.ExecuteQuery($"select OldPath, NewPath, IsNew, IsDeleted, IsRenamed from dbo.RevisionFiles where RevisionId = '{revisionId}'")
                .ToList(x => new FileEntry
                {
                    OldPath = (string) x["OldPath"], 
                    NewPath = (string) x["NewPath"],
                    IsNew = (bool)x["IsNew"],
                    IsRenamed = (bool)x["IsRenamed"],
                    IsDeleted = (bool)x["IsDeleted"],
                });
        }

        private class FileEntry
        {
            public string OldPath { get; set; }
            public string NewPath { get; set; }
            public bool IsNew { get; set; }
            public bool IsRenamed { get; set; }
            public bool IsDeleted { get; set; }
        }

        public override void Down()
        {
            Delete.Table("FileHistory").InSchema("dbo");
        }
    }
}
