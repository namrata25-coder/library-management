using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmLeadManagement.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientCompany",
                table: "WorkInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "WorkInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedDate",
                table: "WorkInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "WorkInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MilestoneId",
                table: "WorkInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "WorkInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RecurringFrequency",
                table: "WorkInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "WorkInvoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "WorkPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    PaymentNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkPayments_WorkInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "WorkInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkRecurringInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    MilestoneId = table.Column<int>(type: "int", nullable: true),
                    ProfileNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextInvoiceDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EndDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalCycles = table.Column<int>(type: "int", nullable: false),
                    CompletedCycles = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkRecurringInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkRecurringInvoices_WorkMilestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "WorkMilestones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkRecurringInvoices_WorkProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "WorkProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkInvoices_MilestoneId",
                table: "WorkInvoices",
                column: "MilestoneId",
                unique: true,
                filter: "[MilestoneId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPayments_InvoiceId",
                table: "WorkPayments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecurringInvoices_MilestoneId",
                table: "WorkRecurringInvoices",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecurringInvoices_ProjectId",
                table: "WorkRecurringInvoices",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkInvoices_WorkMilestones_MilestoneId",
                table: "WorkInvoices",
                column: "MilestoneId",
                principalTable: "WorkMilestones",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkInvoices_WorkMilestones_MilestoneId",
                table: "WorkInvoices");

            migrationBuilder.DropTable(
                name: "WorkPayments");

            migrationBuilder.DropTable(
                name: "WorkRecurringInvoices");

            migrationBuilder.DropIndex(
                name: "IX_WorkInvoices_MilestoneId",
                table: "WorkInvoices");

            migrationBuilder.DropColumn(
                name: "ClientCompany",
                table: "WorkInvoices");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "WorkInvoices");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "WorkInvoices");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "WorkInvoices");

            migrationBuilder.DropColumn(
                name: "MilestoneId",
                table: "WorkInvoices");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "WorkInvoices");

            migrationBuilder.DropColumn(
                name: "RecurringFrequency",
                table: "WorkInvoices");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "WorkInvoices");
        }
    }
}
