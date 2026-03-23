using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Attributes;

public class MaximumAgeAttribute : ValidationAttribute
{
    private readonly int _maxAge;

    public MaximumAgeAttribute(int maxAge)
    {
        _maxAge = maxAge;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString, name, _maxAge);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime date) return new ValidationResult("Data de nascimento inválida.");
        
        int currentYear = DateTime.Today.Year;
        int age = currentYear - date.Year;

        return age > _maxAge 
            ? new ValidationResult(FormatErrorMessage(validationContext.DisplayName)) 
            : ValidationResult.Success;
    }
}