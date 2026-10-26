using QuanLyChiTieuCaNhan.Models;
using System;
using System.Collections.Generic;

namespace QuanLyChiTieuCaNhan.ViewModels
{
    public class ReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Balance => TotalIncome - TotalExpense;
        public List<Transaction> Transactions { get; set; }
    }
}
