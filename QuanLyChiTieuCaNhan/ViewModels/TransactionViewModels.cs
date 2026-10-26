using QuanLyChiTieuCaNhan.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace QuanLyChiTieuCaNhan.ViewModels
{
    public class TransactionCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn loại giao dịch")]
        [Display(Name = "Loại giao dịch")]
        [Range(1, 3)]
        public byte TransactionType { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        [Display(Name = "Số tiền")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ví")]
        [Display(Name = "Ví")]
        public int WalletId { get; set; }

        [Display(Name = "Ví nhận (chuyển khoản)")]
        public int? ToWalletId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày")]
        [Display(Name = "Ngày giao dịch")]
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; } = DateTime.Today;

        [Display(Name = "Ghi chú")]
        [MaxLength(300)]
        public string Description { get; set; }

        public List<SelectListItem> IncomeCategories { get; set; }
        public List<SelectListItem> ExpenseCategories { get; set; }
        public List<SelectListItem> Wallets { get; set; }
    }

    public class TransactionEditViewModel : TransactionCreateViewModel
    {
        public int TransactionId { get; set; }
    }

    public class TransactionListViewModel
    {
        public List<Transaction> Transactions { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public byte? FilterType { get; set; }
        public int? FilterCategoryId { get; set; }
        public int? FilterWalletId { get; set; }
        public DateTime? FilterFromDate { get; set; }
        public DateTime? FilterToDate { get; set; }

        public List<SelectListItem> Categories { get; set; }
        public List<SelectListItem> Wallets { get; set; }
    }
}
