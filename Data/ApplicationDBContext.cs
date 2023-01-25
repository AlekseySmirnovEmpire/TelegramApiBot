using Microsoft.EntityFrameworkCore;
using TelegramApiBot.Data.Entities;

namespace TelegramApiBot.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Question> Questions { get; set; }

    public DbSet<QuestionsToUsers> QuestionsToUsers { get; set; }

    public DbSet<SingleAnket> SingleAnkets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Question>().ToTable("Questions");
        modelBuilder.Entity<QuestionsToUsers>().ToTable("QuestionsToUsers");
        modelBuilder.Entity<SingleAnket>().ToTable("SingleAnket");

        modelBuilder.Entity<QuestionsToUsers>().HasKey(qtu => new { qtu.UserId, qtu.QuestionId });

        modelBuilder.Entity<User>()
            .HasMany(u => u.QuestionsToUsers)
            .WithOne(qtu => qtu.User)
            .HasForeignKey(qtu => qtu.UserId);
        modelBuilder.Entity<Question>()
            .HasMany(q => q.QuestionsToUsers)
            .WithOne(qtu => qtu.Question)
            .HasForeignKey(qtu => qtu.QuestionId);
        modelBuilder.Entity<QuestionsToUsers>()
            .HasOne(qtu => qtu.Question)
            .WithMany(q => q.QuestionsToUsers)
            .HasForeignKey(qtu => qtu.QuestionId);
        modelBuilder.Entity<QuestionsToUsers>()
            .HasOne(qtu => qtu.User)
            .WithMany(u => u.QuestionsToUsers)
            .HasForeignKey(qtu => qtu.UserId);

        modelBuilder.Entity<User>()
            .HasOne(u => u.SingleAnket)
            .WithOne(sa => sa.User)
            .HasForeignKey<SingleAnket>(sa => sa.UserId);

        // modelBuilder.Entity<User>()
        //     .HasMany(u => u.Questions)
        //     .WithMany(q => q.Users)
        //     .UsingEntity<Dictionary<string, object>>(
        //         "QuestionsToUsers",
        //         j => j
        //             .HasOne<Question>()
        //             .WithMany()
        //             .HasForeignKey("QuestionId")
        //             .HasConstraintName("FK_QuestionsToUsers_QuestionId")
        //             .OnDelete(DeleteBehavior.Cascade),
        //         j => j
        //             .HasOne<User>()
        //             .WithMany()
        //             .HasForeignKey("UserId")
        //             .HasConstraintName("FK_QuestionsToUsers_UserId")
        //             .OnDelete(DeleteBehavior.Cascade));
    }
}