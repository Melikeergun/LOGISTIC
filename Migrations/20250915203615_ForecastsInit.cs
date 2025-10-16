using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MLYSO.Web.Migrations
{
    /// <inheritdoc />
    public partial class ForecastsInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemandHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WarehouseId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkuCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandHistories", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandHistories");
        }
    }
}
