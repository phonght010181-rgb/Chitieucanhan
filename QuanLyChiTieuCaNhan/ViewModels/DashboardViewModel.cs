using QuanLyChiTieuCaNhan.Models;
using System.Collections.Generic;

namespace QuanLyChiTieuCaNhan.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance { get; set; }

        public List<CategoryExpense> ExpenseByCategory { get; set; }
        public List<MonthlyData> MonthlyIncomeExpense { get; set; }
        public List<Transaction> RecentTransactions { get; set; }
        public List<BudgetProgress> TopBudgets { get; set; }
    }

    public class CategoryExpense
    {
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthlyData
    {
        public int Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }

    public class BudgetProgress
    {
        public string CategoryName { get; set; }
        public decimal LimitAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public double Percentage => LimitAmount > 0 ? (double)(SpentAmount / LimitAmount * 100) : 0;
    }
}
