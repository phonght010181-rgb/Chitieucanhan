using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChiTieuCaNhan.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; }

        [Required, MaxLength(200)]
        
        public string Email { get; set; }

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; } = "User";

        public bool IsLocked { get; set; } = false;

        public int LoginFailedCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<Category> Categories { get; set; }
        public virtual ICollection<Wallet> Wallets { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
        public virtual ICollection<Budget> Budgets { get; set; }
    }
}
