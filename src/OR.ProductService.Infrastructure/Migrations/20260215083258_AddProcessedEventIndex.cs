using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OR.ProductService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedEventIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_processed_events_ProcessedAt",
                table: "processed_events",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_processed_events_ProcessedAt",
                table: "processed_events");
        }
    }
}
