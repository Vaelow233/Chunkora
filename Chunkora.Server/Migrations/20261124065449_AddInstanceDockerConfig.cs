using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Chunkora.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddInstanceDockerConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "DockerConfig_Environments",
                table: "Instances",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DockerConfig_Hostname",
                table: "Instances",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DockerConfig_Image",
                table: "Instances",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DockerConfig_MaxMemoryBytes",
                table: "Instances",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DockerConfig_MaxNanoCpus",
                table: "Instances",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DockerConfig_Name",
                table: "Instances",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DockerConfig_NetworkMode",
                table: "Instances",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DockerConfig_RestartPolicy",
                table: "Instances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DockerConfig_WorkingDirectory",
                table: "Instances",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Instances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DockerFileBinding",
                columns: table => new
                {
                    InstanceDockerConfigInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HostPath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContainerPath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReadOnly = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockerFileBinding", x => new { x.InstanceDockerConfigInstanceId, x.Id });
                    table.ForeignKey(
                        name: "FK_DockerFileBinding_Instances_InstanceDockerConfigInstanceId",
                        column: x => x.InstanceDockerConfigInstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DockerPortBinding",
                columns: table => new
                {
                    InstanceDockerConfigInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContainerPort = table.Column<long>(type: "bigint", nullable: false),
                    ContainerProtocol = table.Column<int>(type: "integer", nullable: false),
                    HostIp = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HostPort = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockerPortBinding", x => new { x.InstanceDockerConfigInstanceId, x.Id });
                    table.ForeignKey(
                        name: "FK_DockerPortBinding_Instances_InstanceDockerConfigInstanceId",
                        column: x => x.InstanceDockerConfigInstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DockerFileBinding");

            migrationBuilder.DropTable(
                name: "DockerPortBinding");

            migrationBuilder.DropColumn(
                name: "DockerConfig_Environments",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "DockerConfig_Hostname",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "DockerConfig_Image",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "DockerConfig_MaxMemoryBytes",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "DockerConfig_MaxNanoCpus",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "DockerConfig_Name",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "DockerConfig_NetworkMode",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "DockerConfig_RestartPolicy",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "DockerConfig_WorkingDirectory",
                table: "Instances");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Instances");
        }
    }
}
