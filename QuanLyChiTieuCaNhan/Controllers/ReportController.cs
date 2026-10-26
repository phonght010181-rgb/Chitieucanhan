using QuanLyChiTieuCaNhan.Data;
using System;
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
    public class ReportController : Controller
    {
        private readonly AppDbContext db;
        public ReportController(AppDbContext db) => this.db = db;

        public IActionResult Index(DateTime? fromDate, DateTime? toDate)
        {
            int userId = User.GetUserId();

            // Mặc định: tháng hiện tại
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!toDate.HasValue)
                toDate = DateTime.Today;

            var endDate = toDate.Value.Date.AddDays(1);

            var transactions = db.Transactions
                .Include(t => t.Category)
                .Include(t => t.Wallet)
                .Where(t => t.UserId == userId
                    && !t.IsDeleted
                    && t.TransactionDate >= fromDate.Value
                    && t.TransactionDate < endDate)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();

            var model = new ReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalIncome = transactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount),
                TotalExpense = transactions.Where(t => t.TransactionType == 2).Sum(t => t.Amount),
                Transactions = transactions
            };

            return View(model);
        }


    }
}
