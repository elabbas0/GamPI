using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamPI.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "rating",
                table: "Games",
                newName: "Rating");

            migrationBuilder.RenameColumn(
                name: "imgURL",
                table: "Games",
                newName: "ImgURL");

            migrationBuilder.AlterColumn<string>(
                name: "ImgURL",
                table: "Games",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Developer",
                table: "Games",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_Developer",
                table: "Games",
                column: "Developer");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Genre",
                table: "Games",
                column: "Genre");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Title",
                table: "Games",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Games_Developer",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_Genre",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_Title",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Developer",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "Rating",
                table: "Games",
                newName: "rating");

            migrationBuilder.RenameColumn(
                name: "ImgURL",
                table: "Games",
                newName: "imgURL");

            migrationBuilder.AddColumn<string>(
                name: "PasswordSalt",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "imgURL",
                table: "Games",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
