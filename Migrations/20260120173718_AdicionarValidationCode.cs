using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projeto_SEGUES.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarValidationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ValidationCode",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidationCode",
                table: "Tickets");
        }
    }
}
