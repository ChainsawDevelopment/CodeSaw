using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_07_05_18_23)]
    public class Migration_2018_07_05_18_23_AddUserGivenName : Migration
    {
        public override void Up()
        {
            Alter
                .Table("Users")
                .InSchema("dbo")
                .AddColumn("GivenName").AsString(200).Nullable();
        }

        public override void Down()
        {
            Delete.Column("GivenName").FromTable("Users").InSchema("dbo");
        }
    }
}