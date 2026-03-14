using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OscarsBallot.Migrations
{
    /// <inheritdoc />
    public partial class AddBallotEditingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    AppSettingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BallotsLockedOverride = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.AppSettingId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
