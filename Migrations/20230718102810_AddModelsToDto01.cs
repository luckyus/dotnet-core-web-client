using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_core_web_client.Migrations
{
    /// <inheritdoc />
    public partial class AddModelsToDto01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Networks",
                columns: table => new
                {
                    SN = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Interface = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Port = table.Column<int>(type: "int", nullable: true),
                    SslPort = table.Column<int>(type: "int", nullable: true),
                    SubnetMask = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gateway = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DnsStr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ssid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsWireless = table.Column<bool>(type: "bit", nullable: false),
                    IsDhcp = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Networks", x => x.SN);
                });

            migrationBuilder.CreateTable(
                name: "Terminals",
                columns: table => new
                {
                    SN = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasRS485 = table.Column<bool>(type: "bit", nullable: false),
                    MasterServer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoServer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportedCardType = table.Column<int>(type: "int", nullable: true),
                    RegDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terminals", x => x.SN);
                });

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
                name: "Networks");

            migrationBuilder.DropTable(
                name: "Terminals");

            migrationBuilder.DropTable(
                name: "TerminalSettings");
        }
    }
}
