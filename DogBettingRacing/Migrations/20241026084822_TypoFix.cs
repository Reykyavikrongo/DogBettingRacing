using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogBettingRacing.Migrations
{
    /// <inheritdoc />
    public partial class TypoFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WalletBallance",
                table: "Users",
                newName: "WalletBalance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WalletBalance",
                table: "Users",
                newName: "WalletBallance");
        }
    }
}
