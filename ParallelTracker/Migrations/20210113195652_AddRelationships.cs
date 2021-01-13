using Microsoft.EntityFrameworkCore.Migrations;

namespace ParallelTracker.Migrations
{
    public partial class AddRelationships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RepoId",
                table: "Issues",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_RepoId",
                table: "Issues",
                column: "RepoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Repos_RepoId",
                table: "Issues",
                column: "RepoId",
                principalTable: "Repos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Repos_RepoId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_RepoId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "RepoId",
                table: "Issues");
        }
    }
}
