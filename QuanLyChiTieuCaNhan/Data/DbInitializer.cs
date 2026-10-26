using QuanLyChiTieuCaNhan.Models;
namespace QuanLyChiTieuCaNhan.Data;
public static class DbInitializer
{
    public static void SeedSystemCategories(AppDbContext db)
    {
        if (db.Categories.Any(c => c.IsSystem)) return;
        db.Categories.AddRange(new[] { "Ăn uống", "Đi lại", "Nhà ở", "Giải trí", "Sức khỏe", "Mua sắm" }
            .Select(name => new Category { Name = name, CategoryType = 2, IsSystem = true }));
        db.Categories.AddRange(new[] { "Lương", "Kinh doanh", "Khác" }
            .Select(name => new Category { Name = name, CategoryType = 1, IsSystem = true }));
        db.SaveChanges();
    }
    public static void SeedUserCategories(AppDbContext db, int userId)
    {
        var categories = db.Categories.Where(c => c.IsSystem && !c.IsDeleted && c.ParentCategoryId == null).ToList();
        db.Categories.AddRange(categories.Select(c => new Category { UserId = userId, Name = c.Name, CategoryType = c.CategoryType }));
        db.SaveChanges();
    }
}
