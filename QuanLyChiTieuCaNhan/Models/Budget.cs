using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChiTieuCaNhan.Models
{
    [Table("Budgets")]
    public class Budget
    {
        [Key]
        public int BudgetId { get; set; }

        public int UserId { get; set; }

        public int CategoryId { get; set; }

        public decimal LimitAmount { get; set; }

        public decimal SpentAmount { get; set; } = 0;

        public byte Month { get; set; }

        public short Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
    }
}
