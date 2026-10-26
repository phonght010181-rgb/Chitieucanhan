using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChiTieuCaNhan.Models
{
    [Table("Wallets")]
    public class Wallet
    {
        [Key]
        public int WalletId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public byte WalletType { get; set; } // 1=Tiền mặt, 2=Ngân hàng, 3=Ví điện tử

        public decimal Balance { get; set; } = 0;

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [InverseProperty("Wallet")]
        public virtual ICollection<Transaction> Transactions { get; set; }

        [InverseProperty("ToWallet")]
        public virtual ICollection<Transaction> TransferTransactions { get; set; }
    }
}
