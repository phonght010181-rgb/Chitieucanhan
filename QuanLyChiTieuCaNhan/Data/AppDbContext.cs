using Microsoft.EntityFrameworkCore;
using QuanLyChiTieuCaNhan.Models;
namespace QuanLyChiTieuCaNhan.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().HasIndex(x => x.Email).IsUnique();
        model.Entity<Transaction>().HasOne(x => x.Wallet).WithMany(x => x.Transactions).HasForeignKey(x => x.WalletId);
        model.Entity<Transaction>().HasOne(x => x.ToWallet).WithMany(x => x.TransferTransactions).HasForeignKey(x => x.ToWalletId);
        model.Entity<Transaction>().HasOne(x => x.User).WithMany(x => x.Transactions).HasForeignKey(x => x.UserId);
        model.Entity<Transaction>().HasOne(x => x.Category).WithMany(x => x.Transactions).HasForeignKey(x => x.CategoryId);
        model.Entity<Category>().HasOne(x => x.ParentCategory).WithMany(x => x.SubCategories).HasForeignKey(x => x.ParentCategoryId);
        model.Entity<Category>().HasOne(x => x.User).WithMany(x => x.Categories).HasForeignKey(x => x.UserId);
        model.Entity<Wallet>().HasOne(x => x.User).WithMany(x => x.Wallets).HasForeignKey(x => x.UserId);
        model.Entity<Budget>().HasOne(x => x.User).WithMany(x => x.Budgets).HasForeignKey(x => x.UserId);
        model.Entity<Budget>().HasOne(x => x.Category).WithMany(x => x.Budgets).HasForeignKey(x => x.CategoryId);
        model.Entity<Transaction>().Property(x => x.Amount).HasPrecision(18, 2);
        model.Entity<Wallet>().Property(x => x.Balance).HasPrecision(18, 2).IsConcurrencyToken();
        model.Entity<Budget>().Property(x => x.LimitAmount).HasPrecision(18, 2);
        model.Entity<Budget>().Property(x => x.SpentAmount).HasPrecision(18, 2).IsConcurrencyToken();
        model.Entity<User>().Property(x => x.PasswordHash).IsConcurrencyToken();
        model.Entity<User>().Property(x => x.LoginFailedCount).IsConcurrencyToken();
        model.Entity<Budget>().HasIndex(x => new { x.UserId, x.CategoryId, x.Month, x.Year })
            .IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UQ_Budget_User_Cat_Period");
        model.Entity<Transaction>().HasIndex(x => new { x.UserId, x.TransactionDate }).HasDatabaseName("IX_Transactions_UserId_Date");
        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(DateTime)))
                property.SetColumnType("datetime");
        }
    }
}
