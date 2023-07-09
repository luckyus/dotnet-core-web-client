using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_core_web_client.Migrations
{
    /// <inheritdoc />
    public partial class AddModelsToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TerminalSettings",
                columns: table => new
                {
                    SN = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TerminalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateTimeFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TempDetectEnable = table.Column<bool>(type: "bit", nullable: false),
                    FaceDetectEnable = table.Column<bool>(type: "bit", nullable: false),
                    FlashLightEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TempCacheDuration = table.Column<int>(type: "int", nullable: false),
                    AutoUpdateEnabled = table.Column<bool>(type: "bit", nullable: true),
                    AllowedOriginsStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CameraControlStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmartCardControlStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InOutControlStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InOutTiggerStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalDoorRelayControlStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemoteDoorRelayControlStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DailyRebootStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeSyncStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AntiPassbackStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DailySingleAccessStr = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminalSettings", x => x.SN);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerminalSettings");
        }
    }
}
