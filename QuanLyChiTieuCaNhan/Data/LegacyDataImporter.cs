using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using QuanLyChiTieuCaNhan.Models;

namespace QuanLyChiTieuCaNhan.Data;

/// <summary>Reads the legacy database and writes only to a separate, empty Core catalog.</summary>
public static class LegacyDataImporter
{
    public static async Task ImportAsync(AppDbContext db, string sourceConnection, ILogger logger)
    {
        var sourceSettings = new SqlConnectionStringBuilder(sourceConnection);
        var targetSettings = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        if (string.IsNullOrWhiteSpace(sourceSettings.InitialCatalog) ||
            sourceSettings.InitialCatalog.Equals(targetSettings.InitialCatalog, StringComparison.OrdinalIgnoreCase) ||
            !targetSettings.InitialCatalog.EndsWith("_Core", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Import requires a different target catalog ending in _Core.");
        await db.Database.MigrateAsync();
        await using var source = new SqlConnection(sourceConnection);
        await using var target = new SqlConnection(targetSettings.ConnectionString);
        await source.OpenAsync();
        await target.OpenAsync();
        // Prevent legacy writes from changing the copied rows while reading the five tables.
        await using var sourceTransaction = (SqlTransaction)await source.BeginTransactionAsync(IsolationLevel.Serializable);
        await using var targetTransaction = (SqlTransaction)await target.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await using (var command = new SqlCommand("IF EXISTS(SELECT 1 FROM Users) OR EXISTS(SELECT 1 FROM Wallets) OR EXISTS(SELECT 1 FROM Transactions) OR EXISTS(SELECT 1 FROM Budgets) OR EXISTS(SELECT 1 FROM Categories WHERE UserId IS NOT NULL OR IsSystem=0) THROW 51000, 'Target contains user data. Import refused.', 1; DELETE FROM Categories;", target, targetTransaction))
                await command.ExecuteNonQueryAsync();
            foreach (var type in new[] { typeof(User), typeof(Category), typeof(Wallet), typeof(Transaction), typeof(Budget) })
            {
                var entity = db.Model.FindEntityType(type);
                var table = entity.GetTableName();
                var identifier = StoreObjectIdentifier.Table(table, entity.GetSchema());
                var columns = entity.GetProperties().Select(p => p.GetColumnName(identifier)).ToArray();
                // All table and column names come from the application's EF model.
                var names = string.Join(",", columns.Select(c => $"[{c}]"));
                var order = type == typeof(Category) ? " ORDER BY CASE WHEN ParentCategoryId IS NULL THEN 0 ELSE 1 END, CategoryId" : "";
                await using var command = new SqlCommand($"SELECT {names} FROM [dbo].[{table}]{order}", source, sourceTransaction);
                await using var reader = await command.ExecuteReaderAsync();
                using var copy = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls, targetTransaction) { DestinationTableName = $"[dbo].[{table}]" };
                foreach (var column in columns) copy.ColumnMappings.Add(column, column);
                await copy.WriteToServerAsync(reader);
                await reader.CloseAsync();
                await using var check = new SqlCommand($"ALTER TABLE [dbo].[{table}] WITH CHECK CHECK CONSTRAINT ALL; SELECT COUNT_BIG(*) FROM [dbo].[{table}];", target, targetTransaction);
                var count = await check.ExecuteScalarAsync();
                logger.LogInformation("Imported {Table}: {Count} rows", table, count);
            }
            await targetTransaction.CommitAsync();
            await sourceTransaction.CommitAsync();
            db.ChangeTracker.Clear();
            DbInitializer.SeedSystemCategories(db);
        }
        catch
        {
            await targetTransaction.RollbackAsync();
            throw;
        }
    }
}
