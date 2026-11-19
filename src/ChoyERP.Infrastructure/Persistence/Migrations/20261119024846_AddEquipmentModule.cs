using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoyERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "installed_equipment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    installed_date = table.Column<DateOnly>(type: "date", nullable: false),
                    warranty_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installed_equipment", x => x.id);
                    table.CheckConstraint("ck_installed_equipment_warranty", "warranty_end_date IS NULL OR warranty_end_date >= installed_date");
                    table.ForeignKey(
                        name: "fk_installed_equipment_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installed_equipment_installations_installation_id",
                        column: x => x.installation_id,
                        principalTable: "installations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installed_equipment_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_status_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    to_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changed_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_status_changes", x => x.id);
                    table.CheckConstraint("ck_equipment_status_changes_transition", "from_status IS NULL OR from_status <> to_status");
                    table.ForeignKey(
                        name: "fk_equipment_status_changes_installed_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "installed_equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_status_changes_changed_date",
                table: "equipment_status_changes",
                column: "changed_date");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_status_changes_equipment_id",
                table: "equipment_status_changes",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_installed_equipment_client_id",
                table: "installed_equipment",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_installed_equipment_code",
                table: "installed_equipment",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_installed_equipment_installation_id",
                table: "installed_equipment",
                column: "installation_id");

            migrationBuilder.CreateIndex(
                name: "ix_installed_equipment_installed_date",
                table: "installed_equipment",
                column: "installed_date");

            migrationBuilder.CreateIndex(
                name: "ix_installed_equipment_inventory_item_id",
                table: "installed_equipment",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_installed_equipment_inventory_item_id_serial_number",
                table: "installed_equipment",
                columns: new[] { "inventory_item_id", "serial_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_installed_equipment_status",
                table: "installed_equipment",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_installed_equipment_warranty_end_date",
                table: "installed_equipment",
                column: "warranty_end_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_status_changes");

            migrationBuilder.DropTable(
                name: "installed_equipment");
        }
    }
}
