using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_06_07_17_42)]
    public class Migration_2018_06_07_17_42_AddComments : Migration
    {
        public override void Up()
        {
            Create.Table("Comments")
                .InSchema("dbo")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_Comment")
                .WithColumn("ParentId").AsGuid().Nullable()
                .WithColumn("ProjectId").AsInt32().NotNullable()
                .WithColumn("ReviewId").AsInt32().NotNullable()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("Content").AsString().NotNullable()
                .WithColumn("State").AsString().NotNullable()
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("LastUpdatedAt").AsDateTimeOffset().NotNullable();

            Create.ForeignKey("FK_Comments_ParentId_Comments_Id")
                .FromTable("Comments").InSchema("dbo")
                .ForeignColumn("ParentId")
                .ToTable("Comments").InSchema("dbo")
                .PrimaryColumn("Id");

            Create.ForeignKey("FK_Comments_UserId_Users_Id")
                .FromTable("Comments").InSchema("dbo")
                .ForeignColumn("UserId")
                .ToTable("Users").InSchema("dbo")
                .PrimaryColumn("Id");
        }

        public override void Down()
        {
            Delete.ForeignKey("FK_Comments_UserId_Users_Id").OnTable("Comments").InSchema("dbo");
            Delete.ForeignKey("FK_Comments_ParentId_Comments_Id").OnTable("Comments").InSchema("dbo");
            Delete.Table("ReviewComments").InSchema("dbo");
        }
    }
}