using System.ComponentModel.DataAnnotations;
using MailTmBlazor.Application.Forms;

namespace MailTmBlazor.Tests;

public class AuthFormModelTests
{
    [Theory]
    [InlineData("user123", true)]
    [InlineData("ABC_xyz", true)]
    [InlineData("no spaces", false)]
    [InlineData("bad-char!", false)]
    public void Username_validation_respects_allowed_characters(string candidate, bool expectedValid)
    {
        var model = new AuthFormModel
        {
            Username = candidate,
            Password = "secret123" // valid baseline password
        };

        var results = ValidateProperty(model, nameof(AuthFormModel.Username));

        if (expectedValid)
        {
            Assert.Empty(results);
        }
        else
        {
            var error = Assert.Single(results);
            Assert.Contains("letters, numbers, dashes, or underscore", error.ErrorMessage);
        }
    }

    [Theory]
    [InlineData("12345", false)]
    [InlineData("123456", true)]
    [InlineData("abcdef", true)]
    public void Password_requires_minimum_length(string password, bool expectedValid)
    {
        var model = new AuthFormModel
        {
            Username = "validUser",
            Password = password
        };

        var results = ValidateProperty(model, nameof(AuthFormModel.Password));

        if (expectedValid)
        {
            Assert.Empty(results);
        }
        else
        {
            var error = Assert.Single(results);
            Assert.Contains("at least 6 characters", error.ErrorMessage);
        }
    }

    private static IReadOnlyList<ValidationResult> ValidateProperty(object instance, string propertyName)
    {
        var context = new ValidationContext(instance)
        {
            MemberName = propertyName
        };
        var results = new List<ValidationResult>();
        var property = instance.GetType().GetProperty(propertyName)!;
        var value = property.GetValue(instance);

        Validator.TryValidateProperty(value, context, results);
        return results;
    }
}
