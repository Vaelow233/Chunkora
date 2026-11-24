using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chunkora.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInstanceTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InstanceTask",
                table: "InstanceTask");

            migrationBuilder.RenameTable(
                name: "InstanceTask",
                newName: "InstanceTasks");

            migrationBuilder.RenameIndex(
                name: "IX_InstanceTask_Name",
                table: "InstanceTasks",
                newName: "IX_InstanceTasks_Name");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NextRunAt",
                table: "InstanceTasks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastRunAt",
                table: "InstanceTasks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "NodeId",
                table: "InstanceTasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstanceTasks",
                table: "InstanceTasks",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InstanceTasks",
                table: "InstanceTasks");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "InstanceTasks");

            migrationBuilder.RenameTable(
                name: "InstanceTasks",
                newName: "InstanceTask");

            migrationBuilder.RenameIndex(
                name: "IX_InstanceTasks_Name",
                table: "InstanceTask",
                newName: "IX_InstanceTask_Name");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NextRunAt",
                table: "InstanceTask",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastRunAt",
                table: "InstanceTask",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstanceTask",
                table: "InstanceTask",
                column: "Id");
        }
    }
}
