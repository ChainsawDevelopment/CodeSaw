using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_10_05_18_46)]
    public class Migration_2018_10_05_18_46_UserGivenNameAlteredToName : Migration
    {
        public override void Up()
        {
            Rename.Column("GivenName").OnTable("Users").To("Name");
        }

        public override void Down()
        {
            Rename.Column("Name").OnTable("Users").To("GivenName");
        }
    }
}