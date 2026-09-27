using System.ComponentModel.DataAnnotations;

namespace SmartStudyPlannerSystem.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Full name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "Password must contain at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = "";
}