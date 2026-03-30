using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutLoggingAndBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_exercise_stat",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    use_count = table.Column<int>(type: "integer", nullable: false),
                    best_weight_kg = table.Column<double>(type: "double precision", nullable: false),
                    average_weight_kg = table.Column<double>(type: "double precision", nullable: false),
                    last_used_weight_kg = table.Column<double>(type: "double precision", nullable: false),
                    average_timer_in_seconds = table.Column<double>(type: "double precision", nullable: true),
                    average_heart_rate = table.Column<double>(type: "double precision", nullable: true),
                    average_kcal_burned = table.Column<double>(type: "double precision", nullable: true),
                    average_distance_meters = table.Column<double>(type: "double precision", nullable: true),
                    average_speed = table.Column<double>(type: "double precision", nullable: true),
                    average_rate_of_perceived_exertion = table.Column<double>(type: "double precision", nullable: true),
                    last_performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_exercise_stat", x => x.id);
                    table.CheckConstraint("CK_user_exercise_stat_average_weight_non_negative", "average_weight_kg >= 0");
                    table.CheckConstraint("CK_user_exercise_stat_avg_distance_non_negative", "average_distance_meters IS NULL OR average_distance_meters >= 0");
                    table.CheckConstraint("CK_user_exercise_stat_avg_heart_rate_non_negative", "average_heart_rate IS NULL OR average_heart_rate >= 0");
                    table.CheckConstraint("CK_user_exercise_stat_avg_kcal_non_negative", "average_kcal_burned IS NULL OR average_kcal_burned >= 0");
                    table.CheckConstraint("CK_user_exercise_stat_avg_rpe_range", "average_rate_of_perceived_exertion IS NULL OR (average_rate_of_perceived_exertion >= 0 AND average_rate_of_perceived_exertion <= 10)");
                    table.CheckConstraint("CK_user_exercise_stat_avg_speed_non_negative", "average_speed IS NULL OR average_speed >= 0");
                    table.CheckConstraint("CK_user_exercise_stat_avg_timer_non_negative", "average_timer_in_seconds IS NULL OR average_timer_in_seconds >= 0");
                    table.CheckConstraint("CK_user_exercise_stat_best_weight_non_negative", "best_weight_kg >= 0");
                    table.CheckConstraint("CK_user_exercise_stat_last_weight_non_negative", "last_used_weight_kg >= 0");
                    table.CheckConstraint("CK_user_exercise_stat_use_count_non_negative", "use_count >= 0");
                    table.ForeignKey(
                        name: "FK_user_exercise_stat_auth_user_user_id",
                        column: x => x.user_id,
                        principalTable: "auth_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_exercise_stat_exercise_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workout_block",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sets = table.Column<int>(type: "integer", nullable: false),
                    rest_in_seconds = table.Column<int>(type: "integer", nullable: false),
                    instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    order_number = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_block", x => x.id);
                    table.CheckConstraint("CK_workout_block_name_lowercase", "name = lower(name)");
                    table.CheckConstraint("CK_workout_block_order_non_negative", "order_number >= 0");
                    table.CheckConstraint("CK_workout_block_rest_non_negative", "rest_in_seconds >= 0");
                    table.CheckConstraint("CK_workout_block_sets_positive", "sets >= 1");
                    table.ForeignKey(
                        name: "FK_workout_block_auth_user_user_id",
                        column: x => x.user_id,
                        principalTable: "auth_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_session",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    mood = table.Column<int>(type: "integer", nullable: false),
                    feeling = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    duration_in_seconds = table.Column<int>(type: "integer", nullable: false),
                    calories = table.Column<int>(type: "integer", nullable: false),
                    total_kg_moved = table.Column<double>(type: "double precision", nullable: false),
                    total_repetitions = table.Column<int>(type: "integer", nullable: false),
                    average_rate_of_perceived_exertion = table.Column<double>(type: "double precision", nullable: false),
                    performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session", x => x.id);
                    table.CheckConstraint("CK_workout_session_avg_rpe_range", "average_rate_of_perceived_exertion >= 0 AND average_rate_of_perceived_exertion <= 10");
                    table.CheckConstraint("CK_workout_session_calories_non_negative", "calories >= 0");
                    table.CheckConstraint("CK_workout_session_duration_non_negative", "duration_in_seconds >= 0");
                    table.CheckConstraint("CK_workout_session_feeling_lowercase", "feeling = lower(feeling)");
                    table.CheckConstraint("CK_workout_session_mood_range", "mood >= 0 AND mood <= 10");
                    table.CheckConstraint("CK_workout_session_total_kg_non_negative", "total_kg_moved >= 0");
                    table.CheckConstraint("CK_workout_session_total_repetitions_non_negative", "total_repetitions >= 0");
                    table.ForeignKey(
                        name: "FK_workout_session_auth_user_user_id",
                        column: x => x.user_id,
                        principalTable: "auth_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_block_exercise",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workout_block_id = table.Column<int>(type: "integer", nullable: false),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    order_number = table.Column<int>(type: "integer", nullable: false),
                    instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    repetitions = table.Column<int>(type: "integer", nullable: true),
                    timer_in_seconds = table.Column<int>(type: "integer", nullable: true),
                    distance_in_meters = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_block_exercise", x => x.id);
                    table.CheckConstraint("CK_workout_block_exercise_distance_non_negative", "distance_in_meters IS NULL OR distance_in_meters >= 0");
                    table.CheckConstraint("CK_workout_block_exercise_order_non_negative", "order_number >= 0");
                    table.CheckConstraint("CK_workout_block_exercise_repetitions_non_negative", "repetitions IS NULL OR repetitions >= 0");
                    table.CheckConstraint("CK_workout_block_exercise_timer_non_negative", "timer_in_seconds IS NULL OR timer_in_seconds >= 0");
                    table.ForeignKey(
                        name: "FK_workout_block_exercise_exercise_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_block_exercise_workout_block_workout_block_id",
                        column: x => x.workout_block_id,
                        principalTable: "workout_block",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_entry",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workout_session_id = table.Column<int>(type: "integer", nullable: false),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    order_number = table.Column<int>(type: "integer", nullable: false),
                    repetitions = table.Column<int>(type: "integer", nullable: false),
                    mood = table.Column<int>(type: "integer", nullable: false),
                    timer_in_seconds = table.Column<int>(type: "integer", nullable: true),
                    weight_used_kg = table.Column<double>(type: "double precision", nullable: false),
                    rate_of_perceived_exertion = table.Column<double>(type: "double precision", nullable: false),
                    rest_in_seconds = table.Column<int>(type: "integer", nullable: true),
                    kcal_burned = table.Column<int>(type: "integer", nullable: false),
                    distance_in_meters = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    incline = table.Column<int>(type: "integer", nullable: true),
                    speed = table.Column<int>(type: "integer", nullable: true),
                    heart_rate_avg = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_entry", x => x.id);
                    table.CheckConstraint("CK_workout_entry_distance_non_negative", "distance_in_meters IS NULL OR distance_in_meters >= 0");
                    table.CheckConstraint("CK_workout_entry_heart_rate_non_negative", "heart_rate_avg IS NULL OR heart_rate_avg >= 0");
                    table.CheckConstraint("CK_workout_entry_incline_non_negative", "incline IS NULL OR incline >= 0");
                    table.CheckConstraint("CK_workout_entry_kcal_non_negative", "kcal_burned >= 0");
                    table.CheckConstraint("CK_workout_entry_mood_range", "mood >= 0 AND mood <= 10");
                    table.CheckConstraint("CK_workout_entry_order_number_non_negative", "order_number >= 0");
                    table.CheckConstraint("CK_workout_entry_repetitions_non_negative", "repetitions >= 0");
                    table.CheckConstraint("CK_workout_entry_rest_non_negative", "rest_in_seconds IS NULL OR rest_in_seconds >= 0");
                    table.CheckConstraint("CK_workout_entry_rpe_range", "rate_of_perceived_exertion >= 0 AND rate_of_perceived_exertion <= 10");
                    table.CheckConstraint("CK_workout_entry_speed_non_negative", "speed IS NULL OR speed >= 0");
                    table.CheckConstraint("CK_workout_entry_timer_non_negative", "timer_in_seconds IS NULL OR timer_in_seconds >= 0");
                    table.CheckConstraint("CK_workout_entry_weight_non_negative", "weight_used_kg >= 0");
                    table.ForeignKey(
                        name: "FK_workout_entry_exercise_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_entry_workout_session_workout_session_id",
                        column: x => x.workout_session_id,
                        principalTable: "workout_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_exercise_stat_exercise_id",
                table: "user_exercise_stat",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_exercise_stat_user_id_exercise_id",
                table: "user_exercise_stat",
                columns: new[] { "user_id", "exercise_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_block_user_id",
                table: "workout_block",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_block_user_id_name",
                table: "workout_block",
                columns: new[] { "user_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_block_exercise_exercise_id",
                table: "workout_block_exercise",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_block_exercise_workout_block_id_order_number",
                table: "workout_block_exercise",
                columns: new[] { "workout_block_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_entry_exercise_id",
                table: "workout_entry",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_entry_workout_session_id_order_number",
                table: "workout_entry",
                columns: new[] { "workout_session_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_user_id_performed_at_utc",
                table: "workout_session",
                columns: new[] { "user_id", "performed_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_exercise_stat");

            migrationBuilder.DropTable(
                name: "workout_block_exercise");

            migrationBuilder.DropTable(
                name: "workout_entry");

            migrationBuilder.DropTable(
                name: "workout_block");

            migrationBuilder.DropTable(
                name: "workout_session");
        }
    }
}
