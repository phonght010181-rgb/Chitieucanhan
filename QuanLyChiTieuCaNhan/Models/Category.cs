using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChiTieuCaNhan.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        public int? UserId { get; set; }

        public int? ParentCategoryId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public byte CategoryType { get; set; } // 1=Thu, 2=Chi

        public bool IsSystem { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ParentCategoryId")]
        public virtual Category ParentCategory { get; set; }

        public virtual ICollection<Category> SubCategories { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
        public virtual ICollection<Budget> Budgets { get; set; }
    }
}
