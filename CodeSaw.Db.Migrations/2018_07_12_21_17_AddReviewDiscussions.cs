using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_07_12_21_17)]
    public class Migration_2018_07_12_21_17_AddReviewDiscussions : Migration
    {
        public override void Up()
        {
            Create.Table("ReviewDiscussions").InSchema("dbo")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
                .WithColumn("RevisionId").AsGuid().NotNullable().ForeignKey("Revisions", "Id")
                .WithColumn("RootCommentId").AsGuid().NotNullable().ForeignKey("Comments", "Id")
                .WithColumn("LastUpdatedAt").AsDateTimeOffset().NotNullable()
                ;
        }

        public override void Down()
        {
            Delete.Table("ReviewDiscussions").InSchema("dbo");
        }
    }
}