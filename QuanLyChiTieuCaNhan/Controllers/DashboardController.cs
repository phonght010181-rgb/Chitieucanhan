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
    public class DashboardController : Controller
    {
        private readonly AppDbContext db;
        public DashboardController(AppDbContext db) => this.db = db;

        public IActionResult Index()
        {
            int userId = User.GetUserId();
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);
            var startOfYear = new DateTime(now.Year, 1, 1);

            // Tổng thu/chi tháng hiện tại
            var monthTransactions = db.Transactions
                .Where(t => t.UserId == userId
                    && !t.IsDeleted
                    && t.TransactionDate >= startOfMonth
                    && t.TransactionDate < endOfMonth)
                .ToList();

            decimal totalIncome = monthTransactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount);
            decimal totalExpense = monthTransactions.Where(t => t.TransactionType == 2).Sum(t => t.Amount);

            // Biểu đồ tròn: phân bổ chi tiêu theo danh mục tháng hiện tại
            var expenseByCategory = monthTransactions
                .Where(t => t.TransactionType == 2)
                .GroupBy(t => t.CategoryId)
                .Select(g => new CategoryExpense
                {
                    CategoryName = db.Categories.Where(c => c.CategoryId == g.Key).Select(c => c.Name).FirstOrDefault() ?? "N/A",
                    Amount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // Biểu đồ cột: thu chi theo tháng trong năm
            var yearTransactions = db.Transactions
                .Where(t => t.UserId == userId
                    && !t.IsDeleted
                    && t.TransactionDate >= startOfYear
                    && t.TransactionDate < endOfMonth
                    && (t.TransactionType == 1 || t.TransactionType == 2))
                .ToList();

            var monthlyData = new List<MonthlyData>();
            for (int m = 1; m <= now.Month; m++)
            {
                var monthStart = new DateTime(now.Year, m, 1);
                var monthEnd = monthStart.AddMonths(1);
                var monthTrans = yearTransactions.Where(t => t.TransactionDate >= monthStart && t.TransactionDate < monthEnd);

                monthlyData.Add(new MonthlyData
                {
                    Month = m,
                    Income = monthTrans.Where(t => t.TransactionType == 1).Sum(t => t.Amount),
                    Expense = monthTrans.Where(t => t.TransactionType == 2).Sum(t => t.Amount)
                });
            }

            // 10 giao dịch gần nhất
            var recentTransactions = db.Transactions
                .Include(t => t.Category)
                .Include(t => t.Wallet)
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Take(10)
                .ToList();

            // Top 5 ngân sách
            var topBudgets = db.Budgets
                .Where(b => b.UserId == userId
                    && b.Month == (byte)now.Month
                    && b.Year == (short)now.Year
                    && !b.IsDeleted)
                .ToList()
                .Select(b =>
                {
                    var spent = monthTransactions
                        .Where(t => t.CategoryId == b.CategoryId && t.TransactionType == 2)
                        .Sum(t => t.Amount);

                    return new BudgetProgress
                    {
                        CategoryName = db.Categories.Where(c => c.CategoryId == b.CategoryId).Select(c => c.Name).FirstOrDefault() ?? "",
                        LimitAmount = b.LimitAmount,
                        SpentAmount = spent
                    };
                })
                .OrderByDescending(b => b.Percentage)
                .Take(5)
                .ToList();

            var model = new DashboardViewModel
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetBalance = totalIncome - totalExpense,
                ExpenseByCategory = expenseByCategory,
                MonthlyIncomeExpense = monthlyData,
                RecentTransactions = recentTransactions,
                TopBudgets = topBudgets
            };

            return View(model);
        }


    }
}
