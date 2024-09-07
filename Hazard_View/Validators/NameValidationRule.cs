using System.Globalization;
using System.Windows.Controls;

namespace Hazard_View.Validators;

public class NameValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is string text)
            if (text.Length > 0)
                return ValidationResult.ValidResult;
            else
                return new ValidationResult(false, $"Player Name missing.");
        else
            throw new ArgumentException("Input not recognized.");
    }
}
