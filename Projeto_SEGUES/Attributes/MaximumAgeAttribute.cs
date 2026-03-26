using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Attributes;

/// <summary>
/// Custom validation attribute to enforce a maximum age limit based on a birth date.
/// </summary>
/// <remarks>
/// This attribute calculates the age by comparing the provided <see cref="DateTime"/> 
/// with the current system year, ensuring that users do not input dates that would 
/// result in an impossibly high age.
/// </remarks>
public class MaximumAgeAttribute : ValidationAttribute
{
    private readonly int _maxAge;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaximumAgeAttribute"/> class.
    /// </summary>
    /// <param name="maxAge">The maximum allowed age (e.g., 120).</param>
    public MaximumAgeAttribute(int maxAge)
    {
        _maxAge = maxAge;
    }

    /// <summary>
    /// Formats a validation error message by injecting the field name and the maximum age limit.
    /// </summary>
    /// <param name="name">The display name of the validated field.</param>
    /// <returns>A localized or custom error message string.</returns>
    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString, name, _maxAge);
    }

    /// <summary>
    /// Validates whether the specified date of birth results in an age that does not exceed the limit.
    /// </summary>
    /// <param name="value">The value to validate (expected to be a <see cref="DateTime"/>).</param>
    /// <param name="validationContext">The context information about the validation operation.</param>
    /// <returns>
    /// A <see cref="ValidationResult.Success"/> if the age is valid; 
    /// otherwise, a <see cref="ValidationResult"/> with an error message.
    /// </returns>
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