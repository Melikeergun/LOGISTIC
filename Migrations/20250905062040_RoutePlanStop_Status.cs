using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MLYSO.Web.Migrations
{
    /// <inheritdoc />
    public partial class RoutePlanStop_Status : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_Date_Status",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Driver",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DriverUser",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RouteCode",
                table: "Shipments");

            migrationBuilder.RenameColumn(
                name: "Vehicle",
                table: "Shipments",
                newName: "StartAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Shipments",
                newName: "EndAt");

            migrationBuilder.RenameColumn(
                name: "StopsJson",
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

            migrationBuilder.AddColumn<string>(
                name: "ShipNo",
                table: "Shipments",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VehiclePlate",
                table: "Shipments",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "RoutePlanStops",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "RoutePlanStops",
                type: "TEXT",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ShipNo",
                table: "Shipments",
                column: "ShipNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "ShipNo",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "VehiclePlate",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "RoutePlanStops");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "RoutePlanStops");

            migrationBuilder.RenameColumn(
                name: "StartAt",
                table: "Shipments",
                newName: "Vehicle");

            migrationBuilder.RenameColumn(
                name: "EndAt",
                table: "Shipments",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "DriverName",
                table: "Shipments",
                newName: "StopsJson");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Shipments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Shipments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Driver",
                table: "Shipments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DriverUser",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteCode",
                table: "Shipments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Date_Status",
                table: "Shipments",
                columns: new[] { "Date", "Status" });
        }
    }
}
