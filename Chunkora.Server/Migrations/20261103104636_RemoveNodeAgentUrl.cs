using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chunkora.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNodeAgentUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentUrl",
                table: "Nodes");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_AgentToken",
                table: "Nodes",
                column: "AgentToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Nodes_AgentToken",
                table: "Nodes");

            migrationBuilder.AddColumn<string>(
                name: "AgentUrl",
                table: "Nodes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }
    }
}
