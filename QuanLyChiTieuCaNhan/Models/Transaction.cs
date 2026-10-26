using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChiTieuCaNhan.Models
{
    [Table("Transactions")]
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int UserId { get; set; }

        public int CategoryId { get; set; }

        public int WalletId { get; set; }

        public int? ToWalletId { get; set; }

        public decimal Amount { get; set; }

        public byte TransactionType { get; set; } // 1=Thu, 2=Chi, 3=Chuyển khoản

        public DateTime TransactionDate { get; set; }

        [MaxLength(300)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [ForeignKey("WalletId")]
        public virtual Wallet Wallet { get; set; }

        [ForeignKey("ToWalletId")]
        public virtual Wallet ToWallet { get; set; }
    }
}
