using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_07_16_16_48)]
    public class Migration_2018_07_16_16_48_AddUserAvatarUrl : Migration
    {
        public override void Up()
        {
            Alter
                .Table("Users")
                .InSchema("dbo")
                .AddColumn("AvatarUrl").AsString(254).Nullable();
        }

        public override void Down()
        {
            Delete.Column("AvatarUrl").FromTable("Users").InSchema("dbo");
        }
    }
}