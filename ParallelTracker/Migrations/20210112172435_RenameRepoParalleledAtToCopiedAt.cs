using Microsoft.EntityFrameworkCore.Migrations;

namespace ParallelTracker.Migrations
{
    public partial class RenameRepoParalleledAtToCopiedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParalleledAt",
                table: "Repos",
                newName: "CopiedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CopiedAt",
                table: "Repos",
                newName: "ParalleledAt");
        }
    }
}
