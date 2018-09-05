using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_06_13_09_49)]
    public class Migration_2018_06_13_09_49_AddReviewedFiles : Migration
    {
        public override void Up()
        {
            Create
                .Table("ReviewedFiles")
                .InSchema("dbo")
                .WithColumn("ReviewId").AsGuid().NotNullable().ForeignKey("Review", "Id")
                .WithColumn("OldPath").AsString(500).Nullable()
                .WithColumn("NewPath").AsString(500).Nullable()
                ;
        }

        public override void Down()
        {
            Delete.Table("ReviewedFiles").InSchema("dbo");
        }
    }
}