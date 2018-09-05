using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_07_09_21_38)]
    public class Migration_2018_07_09_21_38_AddFileComments : Migration
    {
        public override void Up()
        {
            Create.Table("FileDiscussions").InSchema("dbo")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
                .WithColumn("RevisionId").AsGuid().NotNullable().ForeignKey("Revisions", "Id")
                .WithColumn("RootCommentId").AsGuid().NotNullable().ForeignKey("Comments", "Id")
                .WithColumn("OldPath").AsString()
                .WithColumn("NewPath").AsString()
                .WithColumn("LineNumber").AsInt32().NotNullable()
                .WithColumn("LastUpdatedAt").AsDateTimeOffset().NotNullable()
                ;
        }

        public override void Down()
        {
            Delete.Table("FileDiscussions").InSchema("dbo");
        }
    }
}