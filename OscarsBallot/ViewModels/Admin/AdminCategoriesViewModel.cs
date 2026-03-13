using System.ComponentModel.DataAnnotations;

namespace OscarsBallot.ViewModels.Admin;

public class AdminCategoriesViewModel
{
    public List<AdminCategoryEditItemViewModel> Categories { get; set; } = [];
}

public class AdminCategoryEditItemViewModel
{
    public int CategoryId { get; set; }

    [Required]
    [StringLength(200)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public decimal Points { get; set; }
}
