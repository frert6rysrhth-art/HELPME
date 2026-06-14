using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelovskayaMonitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemChatsAndAvatarColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "Chats",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AvatarColor",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "AvatarColor",
                table: "AspNetUsers");
        }
    }
}
