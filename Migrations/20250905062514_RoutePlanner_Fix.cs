using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MLYSO.Web.Migrations
{
    /// <inheritdoc />
    public partial class RoutePlanner_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_ShipNo",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveredCount",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ReturnCount",
                table: "Shipments");

            migrationBuilder.RenameColumn(
                name: "StartAt",
                table: "Shipments",
                newName: "PlannedDate");

            migrationBuilder.RenameColumn(
                name: "EndAt",
                table: "Shipments",
                newName: "ModifiedAt");

            migrationBuilder.RenameColumn(
                name: "DriverName",
                table: "Shipments",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlannedDate",
                table: "Shipments",
                newName: "StartAt");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "Shipments",
                newName: "EndAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Shipments",
                newName: "DriverName");

            migrationBuilder.AddColumn<int>(
                name: "DeliveredCount",
                table: "Shipments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReturnCount",
                table: "Shipments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ShipNo",
                table: "Shipments",
                column: "ShipNo",
                unique: true);
        }
    }
}
