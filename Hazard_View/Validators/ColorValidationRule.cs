using System.Globalization;
using System.Windows.Controls;

namespace Hazard_View.Validators;

internal class ColorValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value == null)
            return new ValidationResult(false, "A Player Color must be selected.");
        else
            return ValidationResult.ValidResult;
    }
}
