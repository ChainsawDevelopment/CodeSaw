using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_07_09_21_38)]
    public class Migration_2018_07_09_21_38_AddFileComments : Migration
    {
        public override void Up()
        {
            Create.Table("FileDiscussions").InSchema("dbo")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
                .WithColumn("ReviewId").AsGuid().NotNullable().ForeignKey("Review", "Id")
                .WithColumn("OldPath").AsString()
                .WithColumn("NewPath").AsString()
                .WithColumn("LineNumber").AsInt32().NotNullable()
                .WithColumn("LastUpdatedAt").AsDateTimeOffset().NotNullable()
                ;

            Create.Table("FileComments").InSchema("dbo")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
                .WithColumn("FileDiscussionId").AsGuid().NotNullable().ForeignKey("FileDiscussions", "Id")
                .WithColumn("ParentId").AsGuid().Nullable()
                .WithColumn("Content").AsString().NotNullable()
                .WithColumn("State").AsString().NotNullable()
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("LastUpdatedAt").AsDateTimeOffset().NotNullable()
                ;
        }

        public override void Down()
        {
            Delete.Table("FileDiscussions").InSchema("dbo");
            Delete.Table("FileComments").InSchema("dbo");
        }
    }
}