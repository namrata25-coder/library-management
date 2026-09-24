using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrmLeadManagement.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FollowUpSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DefaultReminderDaysBefore = table.Column<int>(type: "integer", nullable: false),
                    EnableEmailReminders = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSmsReminders = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultFollowUpType = table.Column<string>(type: "text", nullable: false),
                    AutoEscalateAfterDaysOverdue = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId = table.Column<string>(type: "character varying(450)", nullable: false),
                    LeadName = table.Column<string>(type: "text", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    MobileNumber = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Designation = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    LeadStatus = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    AssignedTo = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<string>(type: "text", nullable: false),
                    FollowUpDate = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    AssignedDate = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourceOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(450)", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId_FK = table.Column<int>(type: "integer", nullable: false),
                    AssignedEmployee = table.Column<string>(type: "text", nullable: false),
                    AssignmentDate = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentEntry", x => new { x.LeadId_FK, x.Id });
                    table.ForeignKey(
                        name: "FK_AssignmentEntry_Leads_LeadId_FK",
                        column: x => x.LeadId_FK,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CallHistoryEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId_FK = table.Column<int>(type: "integer", nullable: false),
                    CallDate = table.Column<string>(type: "text", nullable: false),
                    CallTime = table.Column<string>(type: "text", nullable: false),
                    CallType = table.Column<string>(type: "text", nullable: false),
                    Duration = table.Column<string>(type: "text", nullable: false),
                    Employee = table.Column<string>(type: "text", nullable: false),
                    CallStatus = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallHistoryEntry", x => new { x.LeadId_FK, x.Id });
                    table.ForeignKey(
                        name: "FK_CallHistoryEntry_Leads_LeadId_FK",
                        column: x => x.LeadId_FK,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Estimations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId_FK = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<string>(type: "text", nullable: false),
                    LeadCustomer = table.Column<string>(type: "text", nullable: false),
                    ProjectRequirement = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<string>(type: "text", nullable: false),
                    Discount = table.Column<string>(type: "text", nullable: false),
                    Tax = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<string>(type: "text", nullable: false),
                    ValidUntil = table.Column<string>(type: "text", nullable: false),
                    TermsConditions = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AttachmentPath = table.Column<string>(type: "text", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estimations", x => new { x.LeadId_FK, x.Id });
                    table.UniqueConstraint("AK_Estimations_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Estimations_Leads_LeadId_FK",
                        column: x => x.LeadId_FK,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FollowUpEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId_FK = table.Column<int>(type: "integer", nullable: false),
                    FollowUpDate = table.Column<string>(type: "text", nullable: false),
                    FollowUpTime = table.Column<string>(type: "text", nullable: false),
                    FollowUpType = table.Column<string>(type: "text", nullable: false),
                    AssignedEmployee = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpEntry", x => new { x.LeadId_FK, x.Id });
                    table.ForeignKey(
                        name: "FK_FollowUpEntry_Leads_LeadId_FK",
                        column: x => x.LeadId_FK,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId_FK = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<string>(type: "text", nullable: false),
                    LeadCustomer = table.Column<string>(type: "text", nullable: false),
                    ProjectRequirement = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ScopeOfWork = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Deliverables = table.Column<string>(type: "text", nullable: false),
                    Timeline = table.Column<string>(type: "text", nullable: false),
                    Budget = table.Column<string>(type: "text", nullable: false),
                    TermsConditions = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AttachmentPath = table.Column<string>(type: "text", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => new { x.LeadId_FK, x.Id });
                    table.ForeignKey(
                        name: "FK_Proposals_Leads_LeadId_FK",
                        column: x => x.LeadId_FK,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId_FK = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<string>(type: "text", nullable: false),
                    LeadCustomer = table.Column<string>(type: "text", nullable: false),
                    ProjectRequirement = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<string>(type: "text", nullable: false),
                    Discount = table.Column<string>(type: "text", nullable: false),
                    Tax = table.Column<string>(type: "text", nullable: false),
                    GrandTotal = table.Column<string>(type: "text", nullable: false),
                    ValidUntil = table.Column<string>(type: "text", nullable: false),
                    TermsConditions = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AttachmentPath = table.Column<string>(type: "text", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => new { x.LeadId_FK, x.Id });
                    table.UniqueConstraint("AK_Quotations_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotations_Leads_LeadId_FK",
                        column: x => x.LeadId_FK,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequirementEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId_FK = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    ExpectedDate = table.Column<string>(type: "text", nullable: false),
                    Budget = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: false),
                    RequirementType = table.Column<string>(type: "text", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementEntry", x => new { x.LeadId_FK, x.Id });
                    table.ForeignKey(
                        name: "FK_RequirementEntry_Leads_LeadId_FK",
                        column: x => x.LeadId_FK,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadId_FK = table.Column<int>(type: "integer", nullable: false),
                    VisitDate = table.Column<string>(type: "text", nullable: false),
                    VisitTime = table.Column<string>(type: "text", nullable: false),
                    VisitLocation = table.Column<string>(type: "text", nullable: false),
                    Employee = table.Column<string>(type: "text", nullable: false),
                    Conversation = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitEntry", x => new { x.LeadId_FK, x.Id });
                    table.ForeignKey(
                        name: "FK_VisitEntry_Leads_LeadId_FK",
                        column: x => x.LeadId_FK,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstimationLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EstimationId_FK = table.Column<int>(type: "integer", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<string>(type: "text", nullable: false),
                    Rate = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationLineItems", x => new { x.EstimationId_FK, x.Id });
                    table.ForeignKey(
                        name: "FK_EstimationLineItems_Estimations_EstimationId_FK",
                        column: x => x.EstimationId_FK,
                        principalTable: "Estimations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotationLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuotationId_FK = table.Column<int>(type: "integer", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<string>(type: "text", nullable: false),
                    Rate = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationLineItems", x => new { x.QuotationId_FK, x.Id });
                    table.ForeignKey(
                        name: "FK_QuotationLineItems_Quotations_QuotationId_FK",
                        column: x => x.QuotationId_FK,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_LeadId",
                table: "Leads",
                column: "LeadId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceOptions_Value",
                table: "SourceOptions",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusOptions_Value",
                table: "StatusOptions",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentEntry");

            migrationBuilder.DropTable(
                name: "CallHistoryEntry");

            migrationBuilder.DropTable(
                name: "EstimationLineItems");

            migrationBuilder.DropTable(
                name: "FollowUpEntry");

            migrationBuilder.DropTable(
                name: "FollowUpSettings");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "QuotationLineItems");

            migrationBuilder.DropTable(
                name: "RequirementEntry");

            migrationBuilder.DropTable(
                name: "SourceOptions");

            migrationBuilder.DropTable(
                name: "StatusOptions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VisitEntry");

            migrationBuilder.DropTable(
                name: "Estimations");

            migrationBuilder.DropTable(
                name: "Quotations");

            migrationBuilder.DropTable(
                name: "Leads");
        }
    }
}
