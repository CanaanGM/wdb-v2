using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingTypesAndMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "measurement",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    hip = table.Column<double>(type: "double precision", nullable: true),
                    chest = table.Column<double>(type: "double precision", nullable: true),
                    waist_under_belly = table.Column<double>(type: "double precision", nullable: true),
                    waist_on_belly = table.Column<double>(type: "double precision", nullable: true),
                    left_thigh = table.Column<double>(type: "double precision", nullable: true),
                    right_thigh = table.Column<double>(type: "double precision", nullable: true),
                    left_calf = table.Column<double>(type: "double precision", nullable: true),
                    right_calf = table.Column<double>(type: "double precision", nullable: true),
                    left_upper_arm = table.Column<double>(type: "double precision", nullable: true),
                    left_forearm = table.Column<double>(type: "double precision", nullable: true),
                    right_upper_arm = table.Column<double>(type: "double precision", nullable: true),
                    right_forearm = table.Column<double>(type: "double precision", nullable: true),
                    neck = table.Column<double>(type: "double precision", nullable: true),
                    minerals = table.Column<double>(type: "double precision", nullable: true),
                    protein = table.Column<double>(type: "double precision", nullable: true),
                    total_body_water = table.Column<double>(type: "double precision", nullable: true),
                    body_fat_mass = table.Column<double>(type: "double precision", nullable: true),
                    body_weight = table.Column<double>(type: "double precision", nullable: true),
                    body_fat_percentage = table.Column<double>(type: "double precision", nullable: true),
                    skeletal_muscle_mass = table.Column<double>(type: "double precision", nullable: true),
                    in_body_score = table.Column<double>(type: "double precision", nullable: true),
                    body_mass_index = table.Column<double>(type: "double precision", nullable: true),
                    basal_metabolic_rate = table.Column<int>(type: "integer", nullable: true),
                    visceral_fat_level = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurement", x => x.id);
                    table.CheckConstraint("CK_measurement_bmr_non_negative", "basal_metabolic_rate IS NULL OR basal_metabolic_rate >= 0");
                    table.CheckConstraint("CK_measurement_user_id_positive", "user_id > 0");
                    table.CheckConstraint("CK_measurement_visceral_fat_non_negative", "visceral_fat_level IS NULL OR visceral_fat_level >= 0");
                    table.ForeignKey(
                        name: "FK_measurement_auth_user_user_id",
                        column: x => x.user_id,
                        principalTable: "auth_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_type", x => x.id);
                    table.CheckConstraint("CK_training_type_name_lowercase", "name = lower(name)");
                });

            migrationBuilder.CreateTable(
                name: "exercise_training_type",
                columns: table => new
                {
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    training_type_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_training_type", x => new { x.training_type_id, x.exercise_id });
                    table.ForeignKey(
                        name: "FK_exercise_training_type_exercise_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exercise_training_type_training_type_training_type_id",
                        column: x => x.training_type_id,
                        principalTable: "training_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_training_type_exercise_id",
                table: "exercise_training_type",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurement_user_id_created_at_utc",
                table: "measurement",
                columns: new[] { "user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_training_type_name",
                table: "training_type",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_training_type");

            migrationBuilder.DropTable(
                name: "measurement");

            migrationBuilder.DropTable(
                name: "training_type");
        }
    }
}
