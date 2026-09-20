using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFeedbackApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordResetOtps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OtpExpiryUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OtpUsed = table.Column<bool>(type: "bit", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    ResetTokenHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetTokenExpiryUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResetTokenUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetOtps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_Email",
                table: "PasswordResetOtps",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetOtps");
        }
    }
}
