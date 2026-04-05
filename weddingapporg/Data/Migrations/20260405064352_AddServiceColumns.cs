using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace weddingapporg.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Services table already exists — only add the missing columns
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Services",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainImage",
                table: "Services",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerId",
                table: "Services",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "temp-organizer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Description", table: "Services");
            migrationBuilder.DropColumn(name: "MainImage",   table: "Services");
            migrationBuilder.DropColumn(name: "OrganizerId", table: "Services");
        }
    }
}
