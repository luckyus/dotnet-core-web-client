﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using dotnet_core_web_client.DBCotexts;

#nullable disable

namespace dotnet_core_web_client.Migrations
{
    [DbContext(typeof(IGuardDBContext))]
    partial class IGuardDBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("dotnet_core_web_client.Models.TerminalSettings", b =>
                {
                    b.Property<string>("SN")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AllowedOriginsStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AntiPassbackStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool?>("AutoUpdateEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("CameraControlStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DailyRebootStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DailySingleAccessStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DateTimeFormat")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("FaceDetectEnable")
                        .HasColumnType("bit");

                    b.Property<bool>("FlashLightEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("InOutControlStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("InOutTiggerStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Language")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LocalDoorRelayControlStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RemoteDoorRelayControlStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SmartCardControlStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("TempCacheDuration")
                        .HasColumnType("int");

                    b.Property<bool>("TempDetectEnable")
                        .HasColumnType("bit");

                    b.Property<string>("TerminalId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TimeSyncStr")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("SN");

                    b.ToTable("TerminalSettings");
                });

            modelBuilder.Entity("dotnet_core_web_client.Models.Terminals", b =>
                {
                    b.Property<string>("SN")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Environment")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirmwareVersion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("HasRS485")
                        .HasColumnType("bit");

                    b.Property<string>("MasterServer")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhotoServer")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("RegDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("SupportedCardType")
                        .HasColumnType("int");

                    b.HasKey("SN");

                    b.ToTable("Terminals");
                });
#pragma warning restore 612, 618
        }
    }
}
