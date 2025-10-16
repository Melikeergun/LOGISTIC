using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MLYSO.Web.Migrations
{
    /// <inheritdoc />
    public partial class TwinInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoxTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    L = table.Column<int>(type: "INTEGER", nullable: false),
                    W = table.Column<int>(type: "INTEGER", nullable: false),
                    H = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgWeightKg = table.Column<double>(type: "REAL", nullable: false),
                    AllowRotateX = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowRotateY = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowRotateZ = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoxTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContainerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    InnerL = table.Column<int>(type: "INTEGER", nullable: false),
                    InnerW = table.Column<int>(type: "INTEGER", nullable: false),
                    InnerH = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPayloadKg = table.Column<double>(type: "REAL", nullable: false),
                    IsULD = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    LengthMm = table.Column<int>(type: "INTEGER", nullable: false),
                    WidthMm = table.Column<int>(type: "INTEGER", nullable: false),
                    HeightMm = table.Column<int>(type: "INTEGER", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackingJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WarehouseId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingJobs_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackingJobContainer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PackingJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContainerTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingJobContainer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingJobContainer_ContainerTypes_ContainerTypeId",
                        column: x => x.ContainerTypeId,
                        principalTable: "ContainerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackingJobContainer_PackingJobs_PackingJobId",
                        column: x => x.PackingJobId,
                        principalTable: "PackingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackingJobItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PackingJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingJobItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingJobItem_BoxTypes_BoxTypeId",
                        column: x => x.BoxTypeId,
                        principalTable: "BoxTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackingJobItem_PackingJobs_PackingJobId",
                        column: x => x.PackingJobId,
                        principalTable: "PackingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PackingJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    VolumeUtilizationPct = table.Column<double>(type: "REAL", nullable: false),
                    WeightUtilizationPct = table.Column<double>(type: "REAL", nullable: false),
                    ContainersUsed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingPlans_PackingJobs_PackingJobId",
                        column: x => x.PackingJobId,
                        principalTable: "PackingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehousePlacement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PackingPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContainerTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    RotDeg = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehousePlacement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehousePlacement_ContainerTypes_ContainerTypeId",
                        column: x => x.ContainerTypeId,
                        principalTable: "ContainerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehousePlacement_PackingPlans_PackingPlanId",
                        column: x => x.PackingPlanId,
                        principalTable: "PackingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BoxPlacement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WarehousePlacementId = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    Z = table.Column<int>(type: "INTEGER", nullable: false),
                    RotX = table.Column<int>(type: "INTEGER", nullable: false),
                    RotY = table.Column<int>(type: "INTEGER", nullable: false),
                    RotZ = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoxPlacement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoxPlacement_BoxTypes_BoxTypeId",
                        column: x => x.BoxTypeId,
                        principalTable: "BoxTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoxPlacement_WarehousePlacement_WarehousePlacementId",
                        column: x => x.WarehousePlacementId,
                        principalTable: "WarehousePlacement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoxPlacement_BoxTypeId",
                table: "BoxPlacement",
                column: "BoxTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BoxPlacement_WarehousePlacementId",
                table: "BoxPlacement",
                column: "WarehousePlacementId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingJobContainer_ContainerTypeId",
                table: "PackingJobContainer",
                column: "ContainerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingJobContainer_PackingJobId",
                table: "PackingJobContainer",
                column: "PackingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingJobItem_BoxTypeId",
                table: "PackingJobItem",
                column: "BoxTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingJobItem_PackingJobId",
                table: "PackingJobItem",
                column: "PackingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingJobs_WarehouseId",
                table: "PackingJobs",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingPlans_PackingJobId",
                table: "PackingPlans",
                column: "PackingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePlacement_ContainerTypeId",
                table: "WarehousePlacement",
                column: "ContainerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehousePlacement_PackingPlanId",
                table: "WarehousePlacement",
                column: "PackingPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoxPlacement");

            migrationBuilder.DropTable(
                name: "PackingJobContainer");

            migrationBuilder.DropTable(
                name: "PackingJobItem");

            migrationBuilder.DropTable(
                name: "WarehousePlacement");

            migrationBuilder.DropTable(
                name: "BoxTypes");

            migrationBuilder.DropTable(
                name: "ContainerTypes");

            migrationBuilder.DropTable(
                name: "PackingPlans");

            migrationBuilder.DropTable(
                name: "PackingJobs");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
