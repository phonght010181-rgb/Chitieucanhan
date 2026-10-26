using QuanLyChiTieuCaNhan.Models;
using System.ComponentModel.DataAnnotations;

namespace QuanLyChiTieuCaNhan.ViewModels
{
    public class WalletCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên ví")]
        [Display(Name = "Tên ví")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại ví")]
        [Display(Name = "Loại ví")]
        [Range(1, 3)]
        public byte WalletType { get; set; }

        [Display(Name = "Số dư ban đầu")]
        [Range(0, double.MaxValue, ErrorMessage = "Số dư phải >= 0")]
        public decimal InitialBalance { get; set; } = 0;
    }

    public class WalletEditViewModel
    {
        public int WalletId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên ví")]
        [Display(Name = "Tên ví")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại ví")]
        [Display(Name = "Loại ví")]
        [Range(1, 3)]
        public byte WalletType { get; set; }

        public bool IsDefault { get; set; }
    }
}
