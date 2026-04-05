using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    difficulty = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    how_to = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise", x => x.Id);
                    table.CheckConstraint("CK_exercise_difficulty", "difficulty >= 0 AND difficulty <= 5");
                    table.CheckConstraint("CK_exercise_name_lowercase", "name = lower(name)");
                });

            migrationBuilder.CreateTable(
                name: "muscle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    muscle_group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    function = table.Column<string>(type: "text", nullable: true),
                    wiki_page_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_muscle", x => x.Id);
                    table.CheckConstraint("CK_muscle_group_lowercase", "muscle_group = lower(muscle_group)");
                    table.CheckConstraint("CK_muscle_name_lowercase", "name = lower(name)");
                });

            migrationBuilder.CreateTable(
                name: "exercise_how_to",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_how_to", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exercise_how_to_exercise_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_muscle",
                columns: table => new
                {
                    muscle_id = table.Column<int>(type: "integer", nullable: false),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_muscle", x => new { x.muscle_id, x.exercise_id });
                    table.ForeignKey(
                        name: "FK_exercise_muscle_exercise_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exercise_muscle_muscle_muscle_id",
                        column: x => x.muscle_id,
                        principalTable: "muscle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_name",
                table: "exercise",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercise_how_to_exercise_id",
                table: "exercise_how_to",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "idx_exercise_muscle_is_primary",
                table: "exercise_muscle",
                column: "is_primary");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_muscle_exercise_id",
                table: "exercise_muscle",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_muscle_name",
                table: "muscle",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_how_to");

            migrationBuilder.DropTable(
                name: "exercise_muscle");

            migrationBuilder.DropTable(
                name: "exercise");

            migrationBuilder.DropTable(
                name: "muscle");
        }
    }
}
