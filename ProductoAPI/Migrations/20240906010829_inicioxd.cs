using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductoAPI.Migrations
{
    /// <inheritdoc />
    public partial class inicioxd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductoSet",
                table: "ProductoSet");

            migrationBuilder.RenameTable(
                name: "ProductoSet",
                newName: "Productos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Productos",
                table: "Productos",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Productos",
                table: "Productos");

            migrationBuilder.RenameTable(
                name: "Productos",
                newName: "ProductoSet");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductoSet",
                table: "ProductoSet",
                column: "Id");
        }
    }
}
