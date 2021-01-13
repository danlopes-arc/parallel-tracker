using Microsoft.EntityFrameworkCore.Migrations;

namespace ParallelTracker.Migrations
{
    public partial class AddRepoToIssue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Repos_RepoId",
                table: "Issues");

            migrationBuilder.AlterColumn<int>(
                name: "RepoId",
                table: "Issues",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Repos_RepoId",
                table: "Issues",
                column: "RepoId",
                principalTable: "Repos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Repos_RepoId",
                table: "Issues");

            migrationBuilder.AlterColumn<int>(
                name: "RepoId",
                table: "Issues",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Repos_RepoId",
                table: "Issues",
                column: "RepoId",
                principalTable: "Repos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
