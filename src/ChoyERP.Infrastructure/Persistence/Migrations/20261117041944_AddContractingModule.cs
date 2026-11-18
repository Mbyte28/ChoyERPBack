using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoyERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    project_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_at = table.Column<DateOnly>(type: "date", nullable: false),
                    responsible_worker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_requests_client_addresses_client_address_id",
                        column: x => x.client_address_id,
                        principalTable: "client_addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_project_requests_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_project_requests_workers_responsible_worker_id",
                        column: x => x.responsible_worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    project_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    decision_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotes", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotes_project_requests_project_request_id",
                        column: x => x.project_request_id,
                        principalTable: "project_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    signed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.id);
                    table.ForeignKey(
                        name: "fk_contracts_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_quotes_quote_id",
                        column: x => x.quote_id,
                        principalTable: "quotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quote_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quote_items", x => x.id);
                    table.CheckConstraint("ck_quote_items_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_quote_items_quotes_quote_id",
                        column: x => x.quote_id,
                        principalTable: "quotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    progress_percent = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expected_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    responsible_worker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.CheckConstraint("ck_projects_progress", "progress_percent BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "fk_projects_client_addresses_client_address_id",
                        column: x => x.client_address_id,
                        principalTable: "client_addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_projects_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projects_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projects_workers_responsible_worker_id",
                        column: x => x.responsible_worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_client_id",
                table: "contracts",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_code",
                table: "contracts",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_quote_id",
                table: "contracts",
                column: "quote_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_status",
                table: "contracts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_project_requests_client_address_id",
                table: "project_requests",
                column: "client_address_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_requests_client_id",
                table: "project_requests",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_requests_code",
                table: "project_requests",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_requests_requested_at",
                table: "project_requests",
                column: "requested_at");

            migrationBuilder.CreateIndex(
                name: "ix_project_requests_responsible_worker_id",
                table: "project_requests",
                column: "responsible_worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_requests_status",
                table: "project_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_projects_client_address_id",
                table: "projects",
                column: "client_address_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_client_id",
                table: "projects",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_code",
                table: "projects",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_contract_id",
                table: "projects",
                column: "contract_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_responsible_worker_id",
                table: "projects",
                column: "responsible_worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_status",
                table: "projects",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_quote_items_quote_id",
                table: "quote_items",
                column: "quote_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_code",
                table: "quotes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotes_project_request_id_version",
                table: "quotes",
                columns: new[] { "project_request_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotes_status",
                table: "quotes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_valid_until",
                table: "quotes",
                column: "valid_until");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "quote_items");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "quotes");

            migrationBuilder.DropTable(
                name: "project_requests");
        }
    }
}
