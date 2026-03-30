using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnAuthUserNormalizedEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM auth_user
                        WHERE normalized_email IS NOT NULL
                        GROUP BY normalized_email
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot apply AddUniqueIndexOnAuthUserNormalizedEmail: duplicate normalized_email values exist in auth_user.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_auth_user_normalized_email",
                table: "auth_user");

            migrationBuilder.CreateIndex(
                name: "IX_auth_user_normalized_email",
                table: "auth_user",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_auth_user_normalized_email",
                table: "auth_user");

            migrationBuilder.CreateIndex(
                name: "IX_auth_user_normalized_email",
                table: "auth_user",
                column: "normalized_email");
        }
    }
}
