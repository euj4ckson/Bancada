using Bancada.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Infrastructure;

public sealed class BancadaDbContext(DbContextOptions<BancadaDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<RecipeComment> RecipeComments => Set<RecipeComment>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<ChallengeIngredient> ChallengeIngredients => Set<ChallengeIngredient>();
    public DbSet<ChallengeSubmission> ChallengeSubmissions => Set<ChallengeSubmission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(80).IsRequired();
            entity.Property(user => user.Bio).HasMaxLength(500);
            entity.Property(user => user.AvatarUrl).HasMaxLength(2000);
        });

        builder.Entity<Recipe>(entity =>
        {
            entity.Property(recipe => recipe.Title).HasMaxLength(140).IsRequired();
            entity.Property(recipe => recipe.Description).HasMaxLength(600).IsRequired();
            entity.Property(recipe => recipe.Instructions).HasMaxLength(8000).IsRequired();
            entity.Property(recipe => recipe.CoverImageUrl).HasMaxLength(2000);
            entity.HasIndex(recipe => recipe.AuthorId);
            entity.HasIndex(recipe => new { recipe.IsPublished, recipe.CreatedAt }).IsDescending(false, true);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(recipe => recipe.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Ingredient>(entity =>
        {
            entity.Property(ingredient => ingredient.Name).HasMaxLength(100).IsRequired();
            entity.Property(ingredient => ingredient.NormalizedName).HasMaxLength(100).IsRequired();
            entity.HasIndex(ingredient => ingredient.NormalizedName).IsUnique();
        });

        builder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(item => new { item.RecipeId, item.IngredientId });
            entity.Property(item => item.Quantity).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Unit).HasMaxLength(30);
            entity.Property(item => item.Notes).HasMaxLength(120);
            entity.HasOne(item => item.Recipe).WithMany(recipe => recipe.Ingredients).HasForeignKey(item => item.RecipeId);
            entity.HasOne(item => item.Ingredient).WithMany().HasForeignKey(item => item.IngredientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Favorite>(entity =>
        {
            entity.HasKey(favorite => new { favorite.UserId, favorite.RecipeId });
            entity.HasIndex(favorite => favorite.RecipeId);
            entity.HasOne(favorite => favorite.Recipe).WithMany(recipe => recipe.Favorites).HasForeignKey(favorite => favorite.RecipeId);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(favorite => favorite.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RecipeComment>(entity =>
        {
            entity.Property(comment => comment.Content).HasMaxLength(1000).IsRequired();
            entity.HasIndex(comment => new { comment.RecipeId, comment.CreatedAt });
            entity.HasOne(comment => comment.Recipe).WithMany(recipe => recipe.Comments).HasForeignKey(comment => comment.RecipeId);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(comment => comment.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Challenge>(entity =>
        {
            entity.Property(challenge => challenge.Title).HasMaxLength(140).IsRequired();
            entity.Property(challenge => challenge.Description).HasMaxLength(1200).IsRequired();
            entity.Property(challenge => challenge.CoverImageUrl).HasMaxLength(2000);
            entity.HasIndex(challenge => new { challenge.Status, challenge.EndsAt });
        });

        builder.Entity<ChallengeIngredient>(entity =>
        {
            entity.HasKey(item => new { item.ChallengeId, item.IngredientId });
            entity.HasOne(item => item.Challenge).WithMany(challenge => challenge.Ingredients).HasForeignKey(item => item.ChallengeId);
            entity.HasOne(item => item.Ingredient).WithMany().HasForeignKey(item => item.IngredientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ChallengeSubmission>(entity =>
        {
            entity.Property(submission => submission.Description).HasMaxLength(500);
            entity.HasIndex(submission => new { submission.ChallengeId, submission.UserId }).IsUnique();
            entity.HasIndex(submission => submission.RecipeId);
            entity.HasOne(submission => submission.Challenge).WithMany(challenge => challenge.Submissions).HasForeignKey(submission => submission.ChallengeId);
            entity.HasOne(submission => submission.Recipe).WithMany().HasForeignKey(submission => submission.RecipeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(submission => submission.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
