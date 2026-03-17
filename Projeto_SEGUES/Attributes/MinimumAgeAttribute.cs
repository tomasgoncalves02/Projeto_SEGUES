using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class MinimumAgeAttribute : ValidationAttribute
{
    private const int MinimumAge = 18;

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime dateOfBirth)
        {
            return new ValidationResult("Data de nascimento inválida.");
        }
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
            age--;
        return age < MinimumAge
            ? new ValidationResult($"A idade mínima é de {MinimumAge} anos.")
            : ValidationResult.Success!;
    }
}
