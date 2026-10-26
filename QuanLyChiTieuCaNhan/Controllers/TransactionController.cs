using QuanLyChiTieuCaNhan.Data;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
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
    public class TransactionController : Controller
    {
        private readonly AppDbContext db;
        public TransactionController(AppDbContext db) => this.db = db;
        private const int PageSize = 20;

        public IActionResult Index(byte? type, int? categoryId, int? walletId,
            DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int userId = User.GetUserId();

            var query = db.Transactions
                .Include(t => t.Category)
                .Include(t => t.Wallet)
                .Include(t => t.ToWallet)
                .Where(t => t.UserId == userId && !t.IsDeleted);

            if (type.HasValue)
                query = query.Where(t => t.TransactionType == type.Value);
            if (categoryId.HasValue)
                query = query.Where(t => t.CategoryId == categoryId.Value);
            if (walletId.HasValue)
                query = query.Where(t => t.WalletId == walletId.Value || t.ToWalletId == walletId.Value);
            if (fromDate.HasValue)
                query = query.Where(t => t.TransactionDate >= fromDate.Value);
            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);
                query = query.Where(t => t.TransactionDate < endDate);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / PageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var transactions = query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var model = new TransactionListViewModel
            {
                Transactions = transactions,
                CurrentPage = page,
                TotalPages = totalPages,
                FilterType = type,
                FilterCategoryId = categoryId,
                FilterWalletId = walletId,
                FilterFromDate = fromDate,
                FilterToDate = toDate,
                Categories = GetAllCategorySelectList(userId),
                Wallets = GetWalletSelectList(userId)
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            int userId = User.GetUserId();
            var model = new TransactionCreateViewModel
            {
                TransactionDate = DateTime.Today,
                IncomeCategories = GetCategorySelectList(userId, 1),
                ExpenseCategories = GetCategorySelectList(userId, 2),
                Wallets = GetWalletSelectList(userId)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TransactionCreateViewModel model)
        {
            int userId = User.GetUserId();
            ValidateReferences(model, userId);

            if (!ModelState.IsValid)
            {
                model.IncomeCategories = GetCategorySelectList(userId, 1);
                model.ExpenseCategories = GetCategorySelectList(userId, 2);
                model.Wallets = GetWalletSelectList(userId);
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    var wallet = db.Wallets.FirstOrDefault(w => w.WalletId == model.WalletId && w.UserId == userId && !w.IsDeleted);
                    if (wallet == null)
                    {
                        ModelState.AddModelError("WalletId", "Ví không hợp lệ");
                        model.IncomeCategories = GetCategorySelectList(userId, 1);
                        model.ExpenseCategories = GetCategorySelectList(userId, 2);
                        model.Wallets = GetWalletSelectList(userId);
                        return View(model);
                    }

                    var trans = new Transaction
                    {
                        UserId = userId,
                        CategoryId = model.CategoryId,
                        WalletId = model.WalletId,
                        Amount = model.Amount,
                        TransactionType = model.TransactionType,
                        TransactionDate = model.TransactionDate,
                        Description = model.Description,
                        CreatedAt = DateTime.Now
                    };

                    // Cập nhật số dư ví
                    switch (model.TransactionType)
                    {
                        case 1: // Thu
                            wallet.Balance += model.Amount;
                            break;
                        case 2: // Chi
                            wallet.Balance -= model.Amount;
                            break;
                        case 3: // Chuyển khoản
                            if (!model.ToWalletId.HasValue || model.ToWalletId == model.WalletId)
                            {
                                ModelState.AddModelError("ToWalletId", "Vui lòng chọn ví nhận khác ví gửi");
                                model.IncomeCategories = GetCategorySelectList(userId, 1);
                                model.ExpenseCategories = GetCategorySelectList(userId, 2);
                                model.Wallets = GetWalletSelectList(userId);
                                return View(model);
                            }
                            var toWallet = db.Wallets.FirstOrDefault(w => w.WalletId == model.ToWalletId && w.UserId == userId && !w.IsDeleted);
                            if (toWallet == null)
                            {
                                ModelState.AddModelError("ToWalletId", "Ví nhận không hợp lệ");
                                model.IncomeCategories = GetCategorySelectList(userId, 1);
                                model.ExpenseCategories = GetCategorySelectList(userId, 2);
                                model.Wallets = GetWalletSelectList(userId);
                                return View(model);
                            }
                            wallet.Balance -= model.Amount;
                            toWallet.Balance += model.Amount;
                            trans.ToWalletId = model.ToWalletId;
                            break;
                    }

                    db.Transactions.Add(trans);

                    // Cập nhật SpentAmount trong Budget nếu là giao dịch Chi
                    if (model.TransactionType == 2)
                    {
                        UpdateBudgetSpent(userId, model.CategoryId, model.TransactionDate, model.Amount);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Thêm giao dịch thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Đã xảy ra lỗi, vui lòng thử lại sau.");
                    model.IncomeCategories = GetCategorySelectList(userId, 1);
                    model.ExpenseCategories = GetCategorySelectList(userId, 2);
                    model.Wallets = GetWalletSelectList(userId);
                    return View(model);
                }
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            int userId = User.GetUserId();
            var trans = db.Transactions.FirstOrDefault(t => t.TransactionId == id && t.UserId == userId && !t.IsDeleted);

            if (trans == null)
                return NotFound();

            var model = new TransactionEditViewModel
            {
                TransactionId = trans.TransactionId,
                TransactionType = trans.TransactionType,
                Amount = trans.Amount,
                CategoryId = trans.CategoryId,
                WalletId = trans.WalletId,
                ToWalletId = trans.ToWalletId,
                TransactionDate = trans.TransactionDate,
                Description = trans.Description,
                IncomeCategories = GetCategorySelectList(userId, 1),
                ExpenseCategories = GetCategorySelectList(userId, 2),
                Wallets = GetWalletSelectList(userId)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(TransactionEditViewModel model)
        {
            int userId = User.GetUserId();
            ValidateReferences(model, userId);

            if (!ModelState.IsValid)
            {
                model.IncomeCategories = GetCategorySelectList(userId, 1);
                model.ExpenseCategories = GetCategorySelectList(userId, 2);
                model.Wallets = GetWalletSelectList(userId);
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    var trans = db.Transactions.FirstOrDefault(t => t.TransactionId == model.TransactionId && t.UserId == userId && !t.IsDeleted);
                    if (trans == null)
                        return NotFound();

                    // Rollback số dư cũ
                    RollbackWalletBalance(trans);

                    // Rollback budget cũ nếu là Chi
                    if (trans.TransactionType == 2)
                    {
                        UpdateBudgetSpent(userId, trans.CategoryId, trans.TransactionDate, -trans.Amount);
                    }

                    // Cập nhật thông tin mới
                    var wallet = db.Wallets.Find(model.WalletId);

                    switch (model.TransactionType)
                    {
                        case 1:
                            wallet.Balance += model.Amount;
                            trans.ToWalletId = null;
                            break;
                        case 2:
                            wallet.Balance -= model.Amount;
                            trans.ToWalletId = null;
                            break;
                        case 3:
                            if (!model.ToWalletId.HasValue || model.ToWalletId == model.WalletId)
                            {
                                ModelState.AddModelError("ToWalletId", "Vui lòng chọn ví nhận khác ví gửi");
                                model.IncomeCategories = GetCategorySelectList(userId, 1);
                                model.ExpenseCategories = GetCategorySelectList(userId, 2);
                                model.Wallets = GetWalletSelectList(userId);
                                return View(model);
                            }
                            var toWallet = db.Wallets.Find(model.ToWalletId);
                            wallet.Balance -= model.Amount;
                            toWallet.Balance += model.Amount;
                            trans.ToWalletId = model.ToWalletId;
                            break;
                    }

                    trans.TransactionType = model.TransactionType;
                    trans.Amount = model.Amount;
                    trans.CategoryId = model.CategoryId;
                    trans.WalletId = model.WalletId;
                    trans.TransactionDate = model.TransactionDate;
                    trans.Description = model.Description;

                    // Cập nhật budget mới nếu là Chi
                    if (model.TransactionType == 2)
                    {
                        UpdateBudgetSpent(userId, model.CategoryId, model.TransactionDate, model.Amount);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Cập nhật giao dịch thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Đã xảy ra lỗi, vui lòng thử lại sau.");
                    model.IncomeCategories = GetCategorySelectList(userId, 1);
                    model.ExpenseCategories = GetCategorySelectList(userId, 2);
                    model.Wallets = GetWalletSelectList(userId);
                    return View(model);
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            int userId = User.GetUserId();

            using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    var trans = db.Transactions.FirstOrDefault(t => t.TransactionId == id && t.UserId == userId && !t.IsDeleted);
                    if (trans == null)
                        return NotFound();

                    // Rollback số dư ví
                    RollbackWalletBalance(trans);

                    // Rollback budget nếu là Chi
                    if (trans.TransactionType == 2)
                    {
                        UpdateBudgetSpent(userId, trans.CategoryId, trans.TransactionDate, -trans.Amount);
                    }

                    trans.IsDeleted = true;
                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Xóa giao dịch thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Đã xảy ra lỗi, vui lòng thử lại sau.";
                    return RedirectToAction("Index");
                }
            }
        }


        private void ValidateReferences(TransactionCreateViewModel model, int userId)
        {
            if (!db.Wallets.Any(w => w.WalletId == model.WalletId && w.UserId == userId && !w.IsDeleted))
                ModelState.AddModelError("WalletId", "Ví không hợp lệ");
            var categoryType = model.TransactionType == 3 ? 2 : model.TransactionType;
            if (!db.Categories.Any(c => c.CategoryId == model.CategoryId && c.UserId == userId && !c.IsDeleted && c.CategoryType == categoryType))
                ModelState.AddModelError("CategoryId", "Danh mục không hợp lệ hoặc không khớp loại giao dịch");
            if (model.TransactionType == 3 && (model.ToWalletId == model.WalletId || !db.Wallets.Any(w => w.WalletId == model.ToWalletId && w.UserId == userId && !w.IsDeleted)))
                ModelState.AddModelError("ToWalletId", "Ví nhận không hợp lệ hoặc trùng ví gửi");
            if (model.TransactionDate < new DateTime(1753, 1, 1))
                ModelState.AddModelError("TransactionDate", "Ngày giao dịch không hợp lệ");
        }

        private void RollbackWalletBalance(Transaction trans)
        {
            var wallet = db.Wallets.Find(trans.WalletId);
            switch (trans.TransactionType)
            {
                case 1: // Thu → rollback trừ
                    wallet.Balance -= trans.Amount;
                    break;
                case 2: // Chi → rollback cộng
                    wallet.Balance += trans.Amount;
                    break;
                case 3: // Chuyển khoản → rollback cả 2 ví
                    wallet.Balance += trans.Amount;
                    if (trans.ToWalletId.HasValue)
                    {
                        var toWallet = db.Wallets.Find(trans.ToWalletId.Value);
                        if (toWallet != null)
                            toWallet.Balance -= trans.Amount;
                    }
                    break;
            }
        }

        private void UpdateBudgetSpent(int userId, int categoryId, DateTime transDate, decimal amount)
        {
            byte month = (byte)transDate.Month;
            short year = (short)transDate.Year;

            var budget = db.Budgets.FirstOrDefault(b =>
                b.UserId == userId &&
                b.CategoryId == categoryId &&
                b.Month == month &&
                b.Year == year &&
                !b.IsDeleted);

            if (budget != null)
            {
                budget.SpentAmount += amount;
                if (budget.SpentAmount < 0)
                    budget.SpentAmount = 0;
            }
        }

        private List<SelectListItem> GetCategorySelectList(int userId, byte type)
        {
            var cats = db.Categories
                .Where(c => c.UserId == userId && c.CategoryType == type && !c.IsDeleted)
                .OrderBy(c => c.ParentCategoryId.HasValue)
                .ThenBy(c => c.Name)
                .ToList();

            return cats.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.ParentCategoryId.HasValue ? "  └ " + c.Name : c.Name
            }).ToList();
        }

        private List<SelectListItem> GetAllCategorySelectList(int userId)
        {
            var items = new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Tất cả --" } };
            items.AddRange(db.Categories
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .OrderBy(c => c.CategoryType)
                .ThenBy(c => c.Name)
                .ToList()
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = $"{c.Name} ({(c.CategoryType == 1 ? "Thu" : "Chi")})"
                }));
            return items;
        }

        private List<SelectListItem> GetWalletSelectList(int userId)
        {
            return db.Wallets
                .Where(w => w.UserId == userId && !w.IsDeleted)
                .OrderBy(w => w.Name)
                .ToList()
                .Select(w => new SelectListItem
                {
                    Value = w.WalletId.ToString(),
                    Text = w.Name
                }).ToList();
        }


    }
}
