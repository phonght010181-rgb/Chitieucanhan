using QuanLyChiTieuCaNhan.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace QuanLyChiTieuCaNhan.ViewModels
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [Display(Name = "Tên danh mục")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại")]
        [Display(Name = "Loại")]
        [Range(1, 2)]
        public byte CategoryType { get; set; }

        [Display(Name = "Danh mục cha")]
        public int? ParentCategoryId { get; set; }

        public List<SelectListItem> ParentCategories { get; set; }
    }

    public class CategoryEditViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [Display(Name = "Tên danh mục")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Display(Name = "Loại")]
        [Range(1, 2)]
        public byte CategoryType { get; set; }

        [Display(Name = "Danh mục cha")]
        public int? ParentCategoryId { get; set; }

        public bool HasTransactions { get; set; }
        public List<SelectListItem> ParentCategories { get; set; }
    }

    public class CategoryListViewModel
    {
        public List<CategoryGroupViewModel> IncomeCategories { get; set; }
        public List<CategoryGroupViewModel> ExpenseCategories { get; set; }
    }

    public class CategoryGroupViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        [Range(1, 2)]
        public byte CategoryType { get; set; }
        public bool IsSystem { get; set; }
        public bool HasTransactions { get; set; }
        public List<CategoryGroupViewModel> SubCategories { get; set; }
    }
}
