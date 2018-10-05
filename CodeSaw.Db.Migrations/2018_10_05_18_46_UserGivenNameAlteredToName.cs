using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_10_05_18_46)]
    public class Migration_2018_10_05_18_46_UserGivenNameAlteredToName : Migration
    {
        public override void Up()
        {
            Alter.Table("Users")
                .InSchema("dbo")
                .AddColumn("Name").AsString(200).Nullable();

            Execute.Sql("UPDATE dbo.Users SET Name = (SELECT GivenName from dbo.Users c where c.Id = Id)");

            Delete.Column("GivenName").FromTable("Users").InSchema("dbo");
        }

        public override void Down()
        {
            Alter.Table("Users")
                .InSchema("dbo")
                .AddColumn("GivenName").AsString(200).Nullable();

            Execute.Sql("UPDATE c SET c.GivenName = d.Name FROM dbo.Users c JOIN dbo.Users d on d.Id = c.Id");

            Delete.Column("Name").FromTable("Users").InSchema("dbo");
        }
    }
}