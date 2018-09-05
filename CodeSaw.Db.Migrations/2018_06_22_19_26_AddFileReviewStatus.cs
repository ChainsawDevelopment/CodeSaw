using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_06_22_19_26)]
    public class Migration_2018_06_22_19_26_AddFileReviewStatus : Migration
    {
        public override void Up()
        {
            Rename.Table("ReviewedFiles").InSchema("dbo").To("ReviewFiles");
            
            Alter.Table("ReviewFiles").InSchema("dbo")
                .AddColumn("Status").AsInt32().NotNullable().WithDefaultValue(1);
        }

        public override void Down()
        {
            Delete.Column("Status").FromTable("ReviewFiles").InSchema("dbo");

            Rename.Table("ReviewFiles").InSchema("dbo").To("ReviewedFiles");
        }
    }
}