using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_06_01_10_46)]
    public class Migration_2018_06_01_10_46_AddReviewRevisions : Migration
    {
        public override void Up()
        {
            Create.Table("Revisions")
                .InSchema("dbo")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_Revision")
                .WithColumn("ProjectId").AsInt32().NotNullable()
                .WithColumn("ReviewId").AsInt32().NotNullable()
                .WithColumn("RevisionNumber").AsInt32().NotNullable()
                .WithColumn("HeadCommit").AsString(40).NotNullable()
                .WithColumn("BaseCommit").AsString(40).NotNullable()
                .WithColumn("LastUpdatedAt").AsDateTimeOffset().NotNullable();
        }

        public override void Down()
        {
            Delete.Table("Revisions");
        }
    }
}