using System.ComponentModel.DataAnnotations;

namespace OscarsBallot.ViewModels.Account;

public class LoginViewModel
{
    [Required]
    [Display(Name = "First Name")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "PIN")]
    [StringLength(4, MinimumLength = 4)]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "PIN must be exactly 4 digits.")]
    public string Pin { get; set; } = string.Empty;
}
