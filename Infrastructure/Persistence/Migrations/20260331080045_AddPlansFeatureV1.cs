using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlansFeatureV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plan_template",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    duration_weeks = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_template", x => x.id);
                    table.CheckConstraint("CK_plan_template_duration_weeks_positive", "duration_weeks >= 1");
                    table.CheckConstraint("CK_plan_template_slug_lowercase", "slug = lower(slug)");
                    table.CheckConstraint("CK_plan_template_status", "status IN ('draft', 'published', 'archived')");
                    table.CheckConstraint("CK_plan_template_version_positive", "version >= 1");
                });

            migrationBuilder.CreateTable(
                name: "plan_day",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan_template_id = table.Column<int>(type: "integer", nullable: false),
                    week_number = table.Column<int>(type: "integer", nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_day", x => x.id);
                    table.CheckConstraint("CK_plan_day_day_range", "day_number >= 1 AND day_number <= 7");
                    table.CheckConstraint("CK_plan_day_week_positive", "week_number >= 1");
                    table.ForeignKey(
                        name: "FK_plan_day_plan_template_plan_template_id",
                        column: x => x.plan_template_id,
                        principalTable: "plan_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_plan_enrollment",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    plan_template_id = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_local_date_inclusive = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_plan_enrollment", x => x.id);
                    table.CheckConstraint("CK_user_plan_enrollment_display_order_non_negative", "display_order >= 0");
                    table.CheckConstraint("CK_user_plan_enrollment_status", "status IN ('active', 'completed', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_user_plan_enrollment_auth_user_user_id",
                        column: x => x.user_id,
                        principalTable: "auth_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_plan_enrollment_plan_template_plan_template_id",
                        column: x => x.plan_template_id,
                        principalTable: "plan_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_day_exercise",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan_day_id = table.Column<int>(type: "integer", nullable: false),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    order_number = table.Column<int>(type: "integer", nullable: false),
                    sets = table.Column<int>(type: "integer", nullable: true),
                    repetitions = table.Column<int>(type: "integer", nullable: true),
                    target_rate_of_perceived_exertion = table.Column<double>(type: "double precision", nullable: true),
                    target_weight_kg = table.Column<double>(type: "double precision", nullable: true),
                    timer_in_seconds = table.Column<int>(type: "integer", nullable: true),
                    distance_in_meters = table.Column<int>(type: "integer", nullable: true),
                    rest_in_seconds = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_day_exercise", x => x.id);
                    table.CheckConstraint("CK_plan_day_exercise_distance_non_negative", "distance_in_meters IS NULL OR distance_in_meters >= 0");
                    table.CheckConstraint("CK_plan_day_exercise_order_non_negative", "order_number >= 0");
                    table.CheckConstraint("CK_plan_day_exercise_repetitions_non_negative", "repetitions IS NULL OR repetitions >= 0");
                    table.CheckConstraint("CK_plan_day_exercise_rest_non_negative", "rest_in_seconds IS NULL OR rest_in_seconds >= 0");
                    table.CheckConstraint("CK_plan_day_exercise_sets_non_negative", "sets IS NULL OR sets >= 0");
                    table.CheckConstraint("CK_plan_day_exercise_target_rpe_range", "target_rate_of_perceived_exertion IS NULL OR (target_rate_of_perceived_exertion >= 0 AND target_rate_of_perceived_exertion <= 10)");
                    table.CheckConstraint("CK_plan_day_exercise_target_weight_non_negative", "target_weight_kg IS NULL OR target_weight_kg >= 0");
                    table.CheckConstraint("CK_plan_day_exercise_timer_non_negative", "timer_in_seconds IS NULL OR timer_in_seconds >= 0");
                    table.ForeignKey(
                        name: "FK_plan_day_exercise_exercise_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plan_day_exercise_plan_day_plan_day_id",
                        column: x => x.plan_day_id,
                        principalTable: "plan_day",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_plan_day_execution",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    enrollment_id = table.Column<int>(type: "integer", nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    linked_workout_session_id = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_plan_day_execution", x => x.id);
                    table.CheckConstraint("CK_user_plan_day_execution_status", "status IN ('scheduled', 'completed', 'skipped', 'partial')");
                    table.ForeignKey(
                        name: "FK_user_plan_day_execution_user_plan_enrollment_enrollment_id",
                        column: x => x.enrollment_id,
                        principalTable: "user_plan_enrollment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_plan_day_execution_workout_session_linked_workout_sess~",
                        column: x => x.linked_workout_session_id,
                        principalTable: "workout_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_plan_exercise_execution",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    day_execution_id = table.Column<int>(type: "integer", nullable: false),
                    plan_day_exercise_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    linked_workout_entry_id = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_plan_exercise_execution", x => x.id);
                    table.CheckConstraint("CK_user_plan_exercise_execution_status", "status IN ('pending', 'completed', 'skipped')");
                    table.ForeignKey(
                        name: "FK_user_plan_exercise_execution_plan_day_exercise_plan_day_exe~",
                        column: x => x.plan_day_exercise_id,
                        principalTable: "plan_day_exercise",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_plan_exercise_execution_user_plan_day_execution_day_ex~",
                        column: x => x.day_execution_id,
                        principalTable: "user_plan_day_execution",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_plan_exercise_execution_workout_entry_linked_workout_e~",
                        column: x => x.linked_workout_entry_id,
                        principalTable: "workout_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plan_day_plan_template_id_week_number_day_number",
                table: "plan_day",
                columns: new[] { "plan_template_id", "week_number", "day_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_day_exercise_exercise_id",
                table: "plan_day_exercise",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_day_exercise_plan_day_id_order_number",
                table: "plan_day_exercise",
                columns: new[] { "plan_day_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_template_slug_version",
                table: "plan_template",
                columns: new[] { "slug", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_template_status",
                table: "plan_template",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_plan_day_execution_enrollment_id_local_date",
                table: "user_plan_day_execution",
                columns: new[] { "enrollment_id", "local_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_plan_day_execution_linked_workout_session_id",
                table: "user_plan_day_execution",
                column: "linked_workout_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_plan_enrollment_plan_template_id",
                table: "user_plan_enrollment",
                column: "plan_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_plan_enrollment_user_id_plan_template_id_status",
                table: "user_plan_enrollment",
                columns: new[] { "user_id", "plan_template_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_user_plan_enrollment_user_id_status",
                table: "user_plan_enrollment",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_user_plan_exercise_execution_day_execution_id_plan_day_exercise_id",
                table: "user_plan_exercise_execution",
                columns: new[] { "day_execution_id", "plan_day_exercise_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_plan_exercise_execution_linked_workout_entry_id",
                table: "user_plan_exercise_execution",
                column: "linked_workout_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_plan_exercise_execution_plan_day_exercise_id",
                table: "user_plan_exercise_execution",
                column: "plan_day_exercise_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_plan_exercise_execution");

            migrationBuilder.DropTable(
                name: "plan_day_exercise");

            migrationBuilder.DropTable(
                name: "user_plan_day_execution");

            migrationBuilder.DropTable(
                name: "plan_day");

            migrationBuilder.DropTable(
                name: "user_plan_enrollment");

            migrationBuilder.DropTable(
                name: "plan_template");
        }
    }
}
