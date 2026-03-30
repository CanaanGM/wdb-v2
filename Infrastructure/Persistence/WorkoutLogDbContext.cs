using Domain.Equipments;
using Domain.Exercises;
using Domain.Muscles;
using Domain.WorkoutBlocks;
using Domain.Workouts;
using Infrastructure.Persistence.Features.Auth.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class WorkoutLogDbContext(DbContextOptions<WorkoutLogDbContext> options)
    : IdentityDbContext<AuthUser, AuthRole, int>(options)
{
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<ExerciseHowTo> ExerciseHowTos => Set<ExerciseHowTo>();

    public DbSet<ExerciseMuscle> ExerciseMuscles => Set<ExerciseMuscle>();

    public DbSet<ExerciseEquipment> ExerciseEquipments => Set<ExerciseEquipment>();

    public DbSet<Muscle> Muscles => Set<Muscle>();

    public DbSet<Equipment> Equipments => Set<Equipment>();

    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();

    public DbSet<WorkoutEntry> WorkoutEntries => Set<WorkoutEntry>();

    public DbSet<UserExerciseStat> UserExerciseStats => Set<UserExerciseStat>();

    public DbSet<WorkoutBlock> WorkoutBlocks => Set<WorkoutBlock>();

    public DbSet<WorkoutBlockExercise> WorkoutBlockExercises => Set<WorkoutBlockExercise>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureIdentityTables(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkoutLogDbContext).Assembly);
    }

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthUser>(builder =>
        {
            builder.ToTable("auth_user", x =>
            {
                x.HasCheckConstraint("CK_auth_user_user_name_lowercase", "user_name = lower(user_name)");
                x.HasCheckConstraint("CK_auth_user_email_lowercase", "email IS NULL OR email = lower(email)");
            });

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.UserName)
                .HasColumnName("user_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.NormalizedUserName)
                .HasColumnName("normalized_user_name")
                .HasMaxLength(255);

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(255);

            builder.Property(x => x.NormalizedEmail)
                .HasColumnName("normalized_email")
                .HasMaxLength(255);

            builder.Property(x => x.EmailConfirmed)
                .HasColumnName("email_confirmed");

            builder.Property(x => x.PasswordHash)
                .HasColumnName("password_hash");

            builder.Property(x => x.SecurityStamp)
                .HasColumnName("security_stamp");

            builder.Property(x => x.ConcurrencyStamp)
                .HasColumnName("concurrency_stamp");

            builder.Property(x => x.PhoneNumber)
                .HasColumnName("phone_number");

            builder.Property(x => x.PhoneNumberConfirmed)
                .HasColumnName("phone_number_confirmed");

            builder.Property(x => x.TwoFactorEnabled)
                .HasColumnName("two_factor_enabled");

            builder.Property(x => x.LockoutEnd)
                .HasColumnName("lockout_end");

            builder.Property(x => x.LockoutEnabled)
                .HasColumnName("lockout_enabled");

            builder.Property(x => x.AccessFailedCount)
                .HasColumnName("access_failed_count");

            builder.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasDefaultValueSql("now()");

            builder.HasIndex(x => x.NormalizedUserName)
                .HasDatabaseName("IX_auth_user_normalized_user_name")
                .IsUnique();

            builder.HasIndex(x => x.NormalizedEmail)
                .HasDatabaseName("IX_auth_user_normalized_email")
                .IsUnique();
        });

        modelBuilder.Entity<AuthRole>(builder =>
        {
            builder.ToTable("auth_role");

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(100);

            builder.Property(x => x.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(100);

            builder.Property(x => x.ConcurrencyStamp)
                .HasColumnName("concurrency_stamp");

            builder.HasIndex(x => x.NormalizedName)
                .HasDatabaseName("IX_auth_role_normalized_name")
                .IsUnique();
        });

        modelBuilder.Entity<IdentityUserRole<int>>(builder =>
        {
            builder.ToTable("auth_user_role");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id");

            builder.Property(x => x.RoleId)
                .HasColumnName("role_id");
        });

        modelBuilder.Entity<IdentityUserClaim<int>>(builder =>
        {
            builder.ToTable("auth_user_claim");

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id");

            builder.Property(x => x.ClaimType)
                .HasColumnName("claim_type");

            builder.Property(x => x.ClaimValue)
                .HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(builder =>
        {
            builder.ToTable("auth_user_login");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id");

            builder.Property(x => x.LoginProvider)
                .HasColumnName("login_provider");

            builder.Property(x => x.ProviderKey)
                .HasColumnName("provider_key");

            builder.Property(x => x.ProviderDisplayName)
                .HasColumnName("provider_display_name");
        });

        modelBuilder.Entity<IdentityUserToken<int>>(builder =>
        {
            builder.ToTable("auth_user_token");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id");

            builder.Property(x => x.LoginProvider)
                .HasColumnName("login_provider");

            builder.Property(x => x.Name)
                .HasColumnName("name");

            builder.Property(x => x.Value)
                .HasColumnName("value");
        });

        modelBuilder.Entity<IdentityRoleClaim<int>>(builder =>
        {
            builder.ToTable("auth_role_claim");

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.RoleId)
                .HasColumnName("role_id");

            builder.Property(x => x.ClaimType)
                .HasColumnName("claim_type");

            builder.Property(x => x.ClaimValue)
                .HasColumnName("claim_value");
        });
    }
}
