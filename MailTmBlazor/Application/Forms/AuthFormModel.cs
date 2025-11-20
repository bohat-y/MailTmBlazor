namespace MailTmBlazor.Application.Forms;

using System.ComponentModel.DataAnnotations;

public sealed class AuthFormModel
{
    [Required]
    [RegularExpression(@"^[A-Za-z0-9_]{3,32}$", ErrorMessage = "Use letters, numbers, or underscore (3–32 chars).")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;
}
