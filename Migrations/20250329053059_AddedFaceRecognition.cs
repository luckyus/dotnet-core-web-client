using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_core_web_client.Migrations
{
    /// <inheritdoc />
    public partial class AddedFaceRecognition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaceRecognitionControlStr",
                table: "TerminalSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceRecognitionControlStr",
                table: "TerminalSettings");
        }
    }
}
