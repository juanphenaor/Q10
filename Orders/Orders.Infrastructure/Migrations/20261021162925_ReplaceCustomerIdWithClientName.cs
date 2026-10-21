using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceCustomerIdWithClientName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Orders"
                ALTER COLUMN "CustomerId" TYPE text
                USING "CustomerId"::text;
                """);

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Orders",
                newName: "ClientName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClientName",
                table: "Orders",
                newName: "CustomerId");

            migrationBuilder.Sql("""
                ALTER TABLE "Orders"
                ALTER COLUMN "CustomerId" TYPE uuid
                USING "CustomerId"::uuid;
                """);
        }
    }
}
