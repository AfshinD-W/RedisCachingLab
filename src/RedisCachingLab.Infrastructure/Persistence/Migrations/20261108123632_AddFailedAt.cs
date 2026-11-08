using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RedisCachingLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFailedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAt",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "OutboxMessages");
        }
    }
}
