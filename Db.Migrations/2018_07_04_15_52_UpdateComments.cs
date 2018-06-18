using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_07_04_15_52)]
    public class Migration_2018_07_04_15_52_UpdateComments : Migration
    {
        public override void Up()
        {
            Delete.ForeignKey("FK_Comments_UserId_Users_Id").OnTable("Comments").InSchema("dbo");

            Delete.Column("ProjectId").FromTable("Comments").InSchema("dbo");
            Delete.Column("ReviewId").FromTable("Comments").InSchema("dbo");
            Delete.Column("UserId").FromTable("Comments").InSchema("dbo");

            Create.Column("ReviewId").OnTable("Comments").InSchema("dbo").AsGuid().NotNullable();
            Create.Column("ChangeKey").OnTable("Comments").InSchema("dbo").AsString().Nullable();
            Create.Column("FilePath").OnTable("Comments").InSchema("dbo").AsString().Nullable();

            Create.ForeignKey("FK_Comments_ReviewId_Review_Id")
                .FromTable("Comments").InSchema("dbo")
                .ForeignColumn("ReviewId")
                .ToTable("Review").InSchema("dbo")
                .PrimaryColumn("Id");
        }

        public override void Down()
        {
            Delete.ForeignKey("FK_Comments_ReviewId_Review_Id").OnTable("Comments").InSchema("dbo");

            Delete.Column("FilePath").FromTable("Comments").InSchema("dbo");
            Delete.Column("ChangeKey").FromTable("Comments").InSchema("dbo");
            Delete.Column("ReviewId").FromTable("Comments").InSchema("dbo");

            Create.Column("UserId").OnTable("Comments").InSchema("dbo").AsInt32().NotNullable();
            Create.Column("ReviewId").OnTable("Comments").InSchema("dbo").AsInt32().NotNullable();
            Create.Column("ProjectId").OnTable("Comments").InSchema("dbo").AsInt32().NotNullable();

            Create.ForeignKey("FK_Comments_UserId_Users_Id")
                .FromTable("Comments").InSchema("dbo")
                .ForeignColumn("UserId")
                .ToTable("Users").InSchema("dbo")
                .PrimaryColumn("Id");
        }
    }
}