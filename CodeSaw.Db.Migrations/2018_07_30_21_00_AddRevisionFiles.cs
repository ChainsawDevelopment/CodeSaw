using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_07_30_21_00)]
    public class Migration_2018_07_30_21_00_AddRevisionFiles : Migration
    {
        public override void Up()
        {
            Create.Table("RevisionFiles")
                .InSchema("dbo")
                .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
                .WithColumn("RevisionId").AsGuid().NotNullable().ForeignKey("Revisions", "Id")
                .WithColumn("LastUpdatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("OldPath").AsString()
                .WithColumn("NewPath").AsString()
                .WithColumn("IsNew").AsBoolean().NotNullable()
                .WithColumn("IsDeleted").AsBoolean().NotNullable()
                .WithColumn("IsRenamed").AsBoolean().NotNullable();
        }

        public override void Down()
        {
            Delete.Table("RevisionFiles").InSchema("dbo");
        }
    }
}