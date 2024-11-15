using System.Globalization;
using System.Windows.Controls;

namespace View.Validators;

public class NameValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is not string text)
            throw new ArgumentException("Input not recognized.");
        if (text.Length > 0)
            return ValidationResult.ValidResult;
        
        return new ValidationResult(false, $"Player Name missing.");
    }
}
