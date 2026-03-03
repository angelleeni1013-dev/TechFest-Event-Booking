using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A2_TechFest_2026.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneNumberToPaymentVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "PaymentVerifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "PaymentVerifications");
        }
    }
}
