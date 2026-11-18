using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoyERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    minimum_stock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    average_unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_items", x => x.id);
                    table.CheckConstraint("ck_inventory_items_average_unit_cost", "average_unit_cost >= 0");
                    table.CheckConstraint("ck_inventory_items_minimum_stock", "minimum_stock >= 0");
                    table.CheckConstraint("ck_inventory_items_quantity_on_hand", "quantity_on_hand >= 0");
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    movement_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    stock_before = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    stock_after = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.CheckConstraint("ck_inventory_movements_quantity", "quantity >= 0");
                    table.CheckConstraint("ck_inventory_movements_stock_after", "stock_after >= 0");
                    table.CheckConstraint("ck_inventory_movements_stock_before", "stock_before >= 0");
                    table.CheckConstraint("ck_inventory_movements_unit_cost", "unit_cost >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_movements_inventory_items_item_id",
                        column: x => x.item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_movements_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_category",
                table: "inventory_items",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_code",
                table: "inventory_items",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_is_active",
                table: "inventory_items",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_name",
                table: "inventory_items",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_code",
                table: "inventory_movements",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_item_id",
                table: "inventory_movements",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_item_id_movement_date",
                table: "inventory_movements",
                columns: new[] { "item_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_movement_date",
                table: "inventory_movements",
                column: "movement_date");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_project_id",
                table: "inventory_movements",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_type",
                table: "inventory_movements",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "inventory_items");
        }
    }
}
