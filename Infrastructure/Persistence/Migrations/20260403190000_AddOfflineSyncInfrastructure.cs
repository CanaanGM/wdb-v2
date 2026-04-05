using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(WorkoutLogDbContext))]
[Migration("20260403190000_AddOfflineSyncInfrastructure")]
public partial class AddOfflineSyncInfrastructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS sync_change_feed (
                seq BIGSERIAL PRIMARY KEY,
                user_id INTEGER NULL,
                entity_type TEXT NOT NULL,
                entity_public_id UUID NOT NULL,
                action TEXT NOT NULL,
                version BIGINT NOT NULL,
                changed_at_utc TIMESTAMPTZ NOT NULL DEFAULT now(),
                deleted_at_utc TIMESTAMPTZ NULL,
                payload JSONB NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_sync_change_feed_user_seq ON sync_change_feed(user_id, seq);
            CREATE INDEX IF NOT EXISTS ix_sync_change_feed_seq ON sync_change_feed(seq);

            CREATE TABLE IF NOT EXISTS sync_push_operation (
                op_id UUID PRIMARY KEY,
                user_id INTEGER NOT NULL,
                result_json JSONB NOT NULL,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT now()
            );

            CREATE INDEX IF NOT EXISTS ix_sync_push_operation_user_created
                ON sync_push_operation(user_id, created_at_utc DESC);

            CREATE TABLE IF NOT EXISTS sync_entity_registry (
                entity_type TEXT PRIMARY KEY,
                table_name TEXT NOT NULL UNIQUE,
                user_scoped BOOLEAN NOT NULL,
                push_enabled BOOLEAN NOT NULL
            );
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO sync_entity_registry(entity_type, table_name, user_scoped, push_enabled)
            VALUES
                ('measurement', 'measurement', true, true),
                ('workout_session', 'workout_session', true, true),
                ('workout_entry', 'workout_entry', true, true),
                ('workout_block', 'workout_block', true, true),
                ('workout_block_exercise', 'workout_block_exercise', true, true),
                ('user_plan_enrollment', 'user_plan_enrollment', true, true),
                ('user_plan_day_execution', 'user_plan_day_execution', true, true),
                ('user_plan_exercise_execution', 'user_plan_exercise_execution', true, true),
                ('exercise', 'exercise', false, false),
                ('muscle', 'muscle', false, false),
                ('equipment', 'equipment', false, false),
                ('training_type', 'training_type', false, false),
                ('plan_template', 'plan_template', false, false),
                ('plan_day', 'plan_day', false, false),
                ('plan_day_exercise', 'plan_day_exercise', false, false),
                ('user_exercise_stat', 'user_exercise_stat', true, false),
                ('exercise_how_to', 'exercise_how_to', false, false),
                ('exercise_muscle', 'exercise_muscle', false, false),
                ('exercise_equipment', 'exercise_equipment', false, false),
                ('exercise_training_type', 'exercise_training_type', false, false)
            ON CONFLICT (entity_type)
            DO UPDATE SET
                table_name = EXCLUDED.table_name,
                user_scoped = EXCLUDED.user_scoped,
                push_enabled = EXCLUDED.push_enabled;
            """);

        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                tbl TEXT;
                has_user BOOLEAN;
            BEGIN
                FOREACH tbl IN ARRAY ARRAY[
                    'measurement',
                    'workout_session',
                    'workout_entry',
                    'workout_block',
                    'workout_block_exercise',
                    'user_plan_enrollment',
                    'user_plan_day_execution',
                    'user_plan_exercise_execution',
                    'exercise',
                    'muscle',
                    'equipment',
                    'training_type',
                    'plan_template',
                    'plan_day',
                    'plan_day_exercise',
                    'user_exercise_stat',
                    'exercise_how_to',
                    'exercise_muscle',
                    'exercise_equipment',
                    'exercise_training_type'
                ]
                LOOP
                    EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS public_id uuid', tbl);
                    EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS updated_at_utc timestamptz', tbl);
                    EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS version bigint', tbl);
                    EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS deleted_at_utc timestamptz', tbl);

                    EXECUTE format('UPDATE %I SET public_id = COALESCE(public_id, gen_random_uuid())', tbl);
                    EXECUTE format('UPDATE %I SET updated_at_utc = COALESCE(updated_at_utc, now())', tbl);
                    EXECUTE format('UPDATE %I SET version = COALESCE(version, 1)', tbl);

                    EXECUTE format('ALTER TABLE %I ALTER COLUMN public_id SET DEFAULT gen_random_uuid()', tbl);
                    EXECUTE format('ALTER TABLE %I ALTER COLUMN updated_at_utc SET DEFAULT now()', tbl);
                    EXECUTE format('ALTER TABLE %I ALTER COLUMN version SET DEFAULT 1', tbl);

                    EXECUTE format('ALTER TABLE %I ALTER COLUMN public_id SET NOT NULL', tbl);
                    EXECUTE format('ALTER TABLE %I ALTER COLUMN updated_at_utc SET NOT NULL', tbl);
                    EXECUTE format('ALTER TABLE %I ALTER COLUMN version SET NOT NULL', tbl);

                    EXECUTE format('CREATE UNIQUE INDEX IF NOT EXISTS ix_%I_pubid ON %I(public_id)', tbl, tbl);
                    EXECUTE format('CREATE INDEX IF NOT EXISTS ix_%I_upd ON %I(updated_at_utc)', tbl, tbl);
                    EXECUTE format('CREATE INDEX IF NOT EXISTS ix_%I_del ON %I(deleted_at_utc)', tbl, tbl);

                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = tbl
                          AND column_name = 'user_id'
                    ) INTO has_user;

                    IF has_user THEN
                        EXECUTE format('CREATE INDEX IF NOT EXISTS ix_%I_usr_upd ON %I(user_id, updated_at_utc DESC)', tbl, tbl);
                    END IF;
                END LOOP;
            END
            $$;
            """);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION sync_set_metadata()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF NEW.public_id IS NULL THEN
                    NEW.public_id := gen_random_uuid();
                END IF;

                IF TG_OP = 'INSERT' THEN
                    NEW.version := COALESCE(NEW.version, 1);
                    NEW.updated_at_utc := COALESCE(NEW.updated_at_utc, now());
                ELSE
                    NEW.version := COALESCE(OLD.version, 0) + 1;
                    NEW.updated_at_utc := now();
                END IF;

                RETURN NEW;
            END;
            $$;
            """);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION sync_capture_change()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            DECLARE
                v_entity_type TEXT;
                v_user_id INTEGER;
                v_action TEXT;
                v_payload JSONB;
                v_deleted_at TIMESTAMPTZ;
                v_version BIGINT;
            BEGIN
                SELECT entity_type
                INTO v_entity_type
                FROM sync_entity_registry
                WHERE table_name = TG_TABLE_NAME;

                IF v_entity_type IS NULL THEN
                    RETURN COALESCE(NEW, OLD);
                END IF;

                IF TG_OP = 'DELETE' THEN
                    v_deleted_at := now();
                    v_payload := to_jsonb(OLD) || jsonb_build_object('deleted_at_utc', v_deleted_at);
                    IF v_payload ? 'user_id' THEN
                        v_user_id := NULLIF(v_payload ->> 'user_id', '')::integer;
                    END IF;

                    v_version := COALESCE((v_payload ->> 'version')::bigint, 0) + 1;

                    INSERT INTO sync_change_feed(
                        user_id,
                        entity_type,
                        entity_public_id,
                        action,
                        version,
                        changed_at_utc,
                        deleted_at_utc,
                        payload)
                    VALUES (
                        v_user_id,
                        v_entity_type,
                        (v_payload ->> 'public_id')::uuid,
                        'delete',
                        v_version,
                        v_deleted_at,
                        v_deleted_at,
                        v_payload);

                    RETURN OLD;
                END IF;

                v_payload := to_jsonb(NEW);
                IF v_payload ? 'user_id' THEN
                    v_user_id := NULLIF(v_payload ->> 'user_id', '')::integer;
                END IF;

                v_action := CASE
                    WHEN TG_OP = 'INSERT' THEN 'create'
                    WHEN NEW.deleted_at_utc IS NOT NULL AND OLD.deleted_at_utc IS NULL THEN 'delete'
                    ELSE 'update'
                END;

                v_version := COALESCE((v_payload ->> 'version')::bigint, 1);

                INSERT INTO sync_change_feed(
                    user_id,
                    entity_type,
                    entity_public_id,
                    action,
                    version,
                    changed_at_utc,
                    deleted_at_utc,
                    payload)
                VALUES (
                    v_user_id,
                    v_entity_type,
                    (v_payload ->> 'public_id')::uuid,
                    v_action,
                    v_version,
                    now(),
                    CASE WHEN v_action = 'delete' THEN now() ELSE NULL END,
                    v_payload);

                RETURN NEW;
            END;
            $$;
            """);

        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                tbl TEXT;
            BEGIN
                FOREACH tbl IN ARRAY ARRAY[
                    'measurement',
                    'workout_session',
                    'workout_entry',
                    'workout_block',
                    'workout_block_exercise',
                    'user_plan_enrollment',
                    'user_plan_day_execution',
                    'user_plan_exercise_execution',
                    'exercise',
                    'muscle',
                    'equipment',
                    'training_type',
                    'plan_template',
                    'plan_day',
                    'plan_day_exercise',
                    'user_exercise_stat',
                    'exercise_how_to',
                    'exercise_muscle',
                    'exercise_equipment',
                    'exercise_training_type'
                ]
                LOOP
                    EXECUTE format('DROP TRIGGER IF EXISTS trg_sync_set_metadata ON %I', tbl);
                    EXECUTE format(
                        'CREATE TRIGGER trg_sync_set_metadata BEFORE INSERT OR UPDATE ON %I FOR EACH ROW EXECUTE FUNCTION sync_set_metadata()',
                        tbl);

                    EXECUTE format('DROP TRIGGER IF EXISTS trg_sync_capture_change ON %I', tbl);
                    EXECUTE format(
                        'CREATE TRIGGER trg_sync_capture_change AFTER INSERT OR UPDATE OR DELETE ON %I FOR EACH ROW EXECUTE FUNCTION sync_capture_change()',
                        tbl);
                END LOOP;
            END
            $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                tbl TEXT;
            BEGIN
                FOREACH tbl IN ARRAY ARRAY[
                    'measurement',
                    'workout_session',
                    'workout_entry',
                    'workout_block',
                    'workout_block_exercise',
                    'user_plan_enrollment',
                    'user_plan_day_execution',
                    'user_plan_exercise_execution',
                    'exercise',
                    'muscle',
                    'equipment',
                    'training_type',
                    'plan_template',
                    'plan_day',
                    'plan_day_exercise',
                    'user_exercise_stat',
                    'exercise_how_to',
                    'exercise_muscle',
                    'exercise_equipment',
                    'exercise_training_type'
                ]
                LOOP
                    EXECUTE format('DROP TRIGGER IF EXISTS trg_sync_set_metadata ON %I', tbl);
                    EXECUTE format('DROP TRIGGER IF EXISTS trg_sync_capture_change ON %I', tbl);

                    EXECUTE format('ALTER TABLE %I DROP COLUMN IF EXISTS deleted_at_utc', tbl);
                    EXECUTE format('ALTER TABLE %I DROP COLUMN IF EXISTS version', tbl);
                    EXECUTE format('ALTER TABLE %I DROP COLUMN IF EXISTS updated_at_utc', tbl);
                    EXECUTE format('ALTER TABLE %I DROP COLUMN IF EXISTS public_id', tbl);
                END LOOP;
            END
            $$;
            """);

        migrationBuilder.Sql("DROP FUNCTION IF EXISTS sync_capture_change;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS sync_set_metadata;");

        migrationBuilder.Sql("DROP TABLE IF EXISTS sync_push_operation;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS sync_change_feed;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS sync_entity_registry;");
    }
}
