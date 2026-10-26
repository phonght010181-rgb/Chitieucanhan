using QuanLyChiTieuCaNhan.Data;
using System;
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
    public class WalletController : Controller
    {
        private readonly AppDbContext db;
        public WalletController(AppDbContext db) => this.db = db;

        public IActionResult Index()
        {
            int userId = User.GetUserId();
            var wallets = db.Wallets
                .Where(w => w.UserId == userId && !w.IsDeleted)
                .OrderByDescending(w => w.IsDefault)
                .ThenBy(w => w.Name)
                .ToList();

            return View(wallets);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new WalletCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(WalletCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int userId = User.GetUserId();

            db.Wallets.Add(new Wallet
            {
                UserId = userId,
                Name = model.Name,
                WalletType = model.WalletType,
                Balance = model.InitialBalance,
                IsDefault = false,
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();

            TempData["Success"] = "Thêm ví thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            int userId = User.GetUserId();
            var wallet = db.Wallets.FirstOrDefault(w => w.WalletId == id && w.UserId == userId && !w.IsDeleted);

            if (wallet == null)
                return NotFound();

            var model = new WalletEditViewModel
            {
                WalletId = wallet.WalletId,
                Name = wallet.Name,
                WalletType = wallet.WalletType,
                IsDefault = wallet.IsDefault
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(WalletEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int userId = User.GetUserId();
            var wallet = db.Wallets.FirstOrDefault(w => w.WalletId == model.WalletId && w.UserId == userId && !w.IsDeleted);

            if (wallet == null)
                return NotFound();

            wallet.Name = model.Name;
            wallet.WalletType = model.WalletType;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật ví thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            int userId = User.GetUserId();
            var wallet = db.Wallets.FirstOrDefault(w => w.WalletId == id && w.UserId == userId && !w.IsDeleted);

            if (wallet == null)
                return NotFound();

            if (wallet.Balance != 0)
            {
                TempData["Error"] = "Không thể xóa ví có số dư khác 0!";
                return RedirectToAction("Index");
            }

            if (db.Transactions.Any(t => (t.WalletId == id || t.ToWalletId == id) && !t.IsDeleted))
            {
                TempData["Error"] = "Không thể xóa ví đã có giao dịch!";
                return RedirectToAction("Index");
            }

            if (wallet.IsDefault)
            {
                TempData["Error"] = "Không thể xóa ví mặc định!";
                return RedirectToAction("Index");
            }

            wallet.IsDeleted = true;
            db.SaveChanges();

            TempData["Success"] = "Xóa ví thành công!";
            return RedirectToAction("Index");
        }


    }
}
