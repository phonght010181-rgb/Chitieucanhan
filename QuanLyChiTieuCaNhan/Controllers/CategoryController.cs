using QuanLyChiTieuCaNhan.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using QuanLyChiTieuCaNhan.Services;
using QuanLyChiTieuCaNhan.Models;
using QuanLyChiTieuCaNhan.ViewModels;

namespace QuanLyChiTieuCaNhan.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly AppDbContext db;
        public CategoryController(AppDbContext db) => this.db = db;

        public IActionResult Index()
        {
            int userId = User.GetUserId();

            var categories = db.Categories
                .Where(c => c.UserId == userId && !c.IsDeleted && c.ParentCategoryId == null)
                .ToList();

            var model = new CategoryListViewModel
            {
                IncomeCategories = BuildCategoryTree(categories.Where(c => c.CategoryType == 1).ToList(), userId),
                ExpenseCategories = BuildCategoryTree(categories.Where(c => c.CategoryType == 2).ToList(), userId)
            };

            return View(model);
        }

        private List<CategoryGroupViewModel> BuildCategoryTree(List<Category> parents, int userId)
        {
            return parents.Select(p => new CategoryGroupViewModel
            {
                CategoryId = p.CategoryId,
                Name = p.Name,
                CategoryType = p.CategoryType,
                IsSystem = p.IsSystem,
                HasTransactions = db.Transactions.Any(t => t.CategoryId == p.CategoryId && !t.IsDeleted),
                SubCategories = db.Categories
                    .Where(c => c.ParentCategoryId == p.CategoryId && c.UserId == userId && !c.IsDeleted)
                    .ToList()
                    .Select(s => new CategoryGroupViewModel
                    {
                        CategoryId = s.CategoryId,
                        Name = s.Name,
                        CategoryType = s.CategoryType,
                        IsSystem = s.IsSystem,
                        HasTransactions = db.Transactions.Any(t => t.CategoryId == s.CategoryId && !t.IsDeleted),
                        SubCategories = new List<CategoryGroupViewModel>()
                    }).ToList()
            }).ToList();
        }

        [HttpGet]
        public IActionResult Create()
        {
            int userId = User.GetUserId();
            var model = new CategoryCreateViewModel
            {
                ParentCategories = GetParentCategorySelectList(userId, null)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CategoryCreateViewModel model)
        {
            int userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                model.ParentCategories = GetParentCategorySelectList(userId, null);
                return View(model);
            }

            // Nếu chọn danh mục cha, check loại khớp
            if (model.ParentCategoryId.HasValue)
            {
                var parent = db.Categories.Find(model.ParentCategoryId.Value);
                if (parent == null || parent.UserId != userId || parent.IsDeleted || parent.ParentCategoryId != null || parent.CategoryType != model.CategoryType)
                {
                    ModelState.AddModelError("ParentCategoryId", "Danh mục cha không hợp lệ");
                    model.ParentCategories = GetParentCategorySelectList(userId, null);
                    return View(model);
                }
            }

            db.Categories.Add(new Category
            {
                UserId = userId,
                Name = model.Name,
                CategoryType = model.CategoryType,
                ParentCategoryId = model.ParentCategoryId
            });
            db.SaveChanges();

            TempData["Success"] = "Thêm danh mục thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            int userId = User.GetUserId();
            var category = db.Categories.FirstOrDefault(c => c.CategoryId == id && c.UserId == userId && !c.IsDeleted);

            if (category == null)
                return NotFound();

            var model = new CategoryEditViewModel
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                CategoryType = category.CategoryType,
                ParentCategoryId = category.ParentCategoryId,
                HasTransactions = db.Transactions.Any(t => t.CategoryId == id && !t.IsDeleted),
                ParentCategories = GetParentCategorySelectList(userId, id)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CategoryEditViewModel model)
        {
            int userId = User.GetUserId();

            if (!ModelState.IsValid)
            {
                model.ParentCategories = GetParentCategorySelectList(userId, model.CategoryId);
                return View(model);
            }

            var category = db.Categories.FirstOrDefault(c => c.CategoryId == model.CategoryId && c.UserId == userId && !c.IsDeleted);
            if (category == null)
                return NotFound();

            if (model.ParentCategoryId != category.ParentCategoryId && model.ParentCategoryId.HasValue &&
                (!db.Categories.Any(c => c.CategoryId == model.ParentCategoryId && c.UserId == userId && !c.IsDeleted && c.ParentCategoryId == null && c.CategoryType == category.CategoryType && c.CategoryId != category.CategoryId)
                || db.Categories.Any(c => c.ParentCategoryId == category.CategoryId && !c.IsDeleted)))
            {
                ModelState.AddModelError("ParentCategoryId", "Danh mục cha không hợp lệ hoặc sẽ tạo cấu trúc quá hai cấp");
                model.ParentCategories = GetParentCategorySelectList(userId, model.CategoryId);
                return View(model);
            }
            category.Name = model.Name;
            category.ParentCategoryId = model.ParentCategoryId;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật danh mục thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            int userId = User.GetUserId();
            var category = db.Categories.FirstOrDefault(c => c.CategoryId == id && c.UserId == userId && !c.IsDeleted);

            if (category == null)
                return NotFound();

            if (db.Transactions.Any(t => t.CategoryId == id && !t.IsDeleted))
            {
                TempData["Error"] = "Không thể xóa danh mục đã có giao dịch!";
                return RedirectToAction("Index");
            }

            var childIds = db.Categories.Where(c => c.ParentCategoryId == id && c.UserId == userId && !c.IsDeleted).Select(c => c.CategoryId).ToList();
            childIds.Add(id);
            if (db.Transactions.Any(t => childIds.Contains(t.CategoryId) && !t.IsDeleted) || db.Budgets.Any(b => childIds.Contains(b.CategoryId) && !b.IsDeleted))
            {
                TempData["Error"] = "Không thể xóa danh mục hoặc danh mục con có giao dịch/ngân sách!";
                return RedirectToAction("Index");
            }
            // Xóa danh mục con
            var subCats = db.Categories.Where(c => c.ParentCategoryId == id && !c.IsDeleted).ToList();
            foreach (var sc in subCats)
            {
                sc.IsDeleted = true;
            }

            category.IsDeleted = true;
            db.SaveChanges();

            TempData["Success"] = "Xóa danh mục thành công!";
            return RedirectToAction("Index");
        }

        private List<SelectListItem> GetParentCategorySelectList(int userId, int? excludeId)
        {
            var parents = db.Categories
                .Where(c => c.UserId == userId && !c.IsDeleted && c.ParentCategoryId == null)
                .ToList();

            if (excludeId.HasValue)
                parents = parents.Where(c => c.CategoryId != excludeId.Value).ToList();

            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Không có (danh mục gốc) --" }
            };

            items.AddRange(parents.Select(p => new SelectListItem
            {
                Value = p.CategoryId.ToString(),
                Text = $"{p.Name} ({(p.CategoryType == 1 ? "Thu" : "Chi")})"
            }));

            return items;
        }


    }
}
