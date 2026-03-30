using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Attributes;

/// <summary>
/// Custom validation attribute to enforce a minimum age requirement (default is 18 years).
/// </summary>
/// <remarks>
/// This validator performs a precise age calculation by checking if the user's 
/// birthday has already occurred in the current calendar year.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class MinimumAgeAttribute : ValidationAttribute
{
    private const int MinimumAge = 18;

    /// <summary>
    /// Validates that the provided date of birth corresponds to an individual 
    /// who is at least 18 years old.
    /// </summary>
    /// <param name="value">The value to validate (expected to be a <see cref="DateTime"/>).</param>
    /// <param name="validationContext">The context information about the validation operation.</param>
    /// <returns>
    /// A <see cref="ValidationResult.Success"/> if the user meets the age requirement; 
    /// otherwise, a <see cref="ValidationResult"/> with a localized error message.
    /// </returns>
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime dateOfBirth) return new ValidationResult("Data de nascimento inválida.");

        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;

        // Adjust age if the birthday hasn't happened yet this year
        if (dateOfBirth > today.AddYears(-age))
            age--;

        return age < MinimumAge
            ? new ValidationResult($"A idade mínima é de {MinimumAge} anos.")
            : ValidationResult.Success!;
    }
}