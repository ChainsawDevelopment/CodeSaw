using System.Data;
using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_12_01_16_41)]
    public class Migration_2018_12_01_16_41_AddFileIdToFileDiscussion : Migration
    {
        public override void Up()
        {
            Alter.Table("FileDiscussions").InSchema("dbo")
                .AddColumn("FileId").AsGuid().Nullable();

            Execute.Sql(@"
update fd
set
    FileId = fh.FileId
from dbo.FileDiscussions fd
join dbo.Revisions rev on rev.Id = fd.RevisionId
join dbo.Comments rc on rc.Id = fd.RootCommentId
join dbo.FileHistory fh on fh.RevisionId = rev.Id and fh.FileName = fd.NewPath
where rev.LastUpdatedAt >= (select MIN(LastUpdatedAt) from dbo.RevisionFiles)
");
        }

        public override void Down()
        {
            Delete.Column("FileId")
                .FromTable("FileDiscussions").InSchema("dbo");
        }
    }
}
