using System.Data;
using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_11_06_15_08)]
    public class Migration_2018_11_06_15_08_FileIdInReviewFiles : Migration
    {
        public override void Up()
        {
            Execute.Sql("delete from dbo.ReviewFiles where ReviewId in (select Id from dbo.Review where RevisionId not in (select Id from dbo.Revisions))");
            Execute.Sql("delete from dbo.Comments where ReviewId in (select Id from dbo.Review where RevisionId not in (select Id from dbo.Revisions))");
            Execute.Sql("delete from dbo.Review where RevisionId not in (select Id from dbo.Revisions)");

            Create.ForeignKey()
                .FromTable("Review").InSchema("dbo")
                .ForeignColumn("RevisionId")
                .ToTable("Revisions").InSchema("dbo").PrimaryColumn("Id");

            Alter.Table("ReviewFiles").InSchema("dbo")
                .AddColumn("FileId").AsGuid().Nullable();

           Execute.Sql(@"
update rf
set
    FileId = fh.FileId
from dbo.ReviewFiles rf
join dbo.Review review on review.Id = rf.ReviewId
join dbo.Revisions revision on revision.Id = review.RevisionId
join dbo.FileHistory fh on fh.RevisionId = revision.Id and rf.NewPath = fh.FileName
where review.LastUpdatedAt >= (select MIN(LastUpdatedAt) from dbo.RevisionFiles)
");
        }

        public override void Down()
        {
            Delete.ForeignKey()
                .FromTable("Review").InSchema("dbo")
                .ForeignColumn("RevisionId")
                .ToTable("Revisions").PrimaryColumn("Id");

            Delete.Column("FileId").FromTable("ReviewFiles").InSchema("dbo");
        }
    }
}