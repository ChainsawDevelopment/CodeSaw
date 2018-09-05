using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_06_11_17_37)]
    public class Migration_2018_06_11_17_37_AddReview : Migration
    {
        public override void Up()
        {
            Create.Table("Review")
                .InSchema("dbo")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
                .WithColumn("UserId").AsInt32().NotNullable().ForeignKey("FK_Review_User", "dbo", "Users", "Id")
                .WithColumn("RevisionId").AsGuid().NotNullable()
                .WithColumn("ReviewedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("LastUpdatedAt").AsDateTimeOffset().NotNullable()
                ;
        }

        public override void Down()
        {
            Delete.Table("Review").InSchema("dbo");
        }
    }
}