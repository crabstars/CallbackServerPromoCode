using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallbackServerPromoCodes.Migrations
{
    /// <inheritdoc />
    public partial class ActivatedForChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activated",
                table: "Channels",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activated",
                table: "Channels");
        }
    }
}
