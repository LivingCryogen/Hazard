using System.Globalization;
using System.Windows.Controls;

namespace View.Validators;

internal class ColorValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value != null)
            return ValidationResult.ValidResult;
        return new ValidationResult(false, "A Player Color must be selected.");
    }
}
