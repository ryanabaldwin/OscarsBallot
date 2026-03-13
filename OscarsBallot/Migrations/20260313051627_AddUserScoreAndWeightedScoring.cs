using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OscarsBallot.Migrations
{
    /// <inheritdoc />
    public partial class AddUserScoreAndWeightedScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Points",
                table: "Categories",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "Categories"
                SET "Points" = COALESCE(
                    (SELECT "Points" FROM "Winners" WHERE "Winners"."CategoryId" = "Categories"."CategoryId"),
                    "Categories"."Points"
                );
                """);

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Winners");

            migrationBuilder.CreateIndex(
                name: "IX_Ballots_UserId_CategoryId_NomineeId",
                table: "Ballots",
                columns: new[] { "UserId", "CategoryId", "NomineeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ballots_UserId_CategoryId_NomineeId",
                table: "Ballots");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Categories");

            migrationBuilder.AddColumn<decimal>(
                name: "Points",
                table: "Winners",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "Winners"
                SET "Points" = COALESCE(
                    (SELECT "Points" FROM "Categories" WHERE "Categories"."CategoryId" = "Winners"."CategoryId"),
                    "Winners"."Points"
                );
                """);
        }
    }
}
