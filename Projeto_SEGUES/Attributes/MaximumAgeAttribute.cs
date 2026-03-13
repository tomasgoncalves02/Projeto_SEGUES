using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Attributes
{
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
            if (value is DateTime date)
            {
                int currentYear = DateTime.Today.Year;
                int age = currentYear - date.Year;

                if (age > _maxAge)
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }

                return ValidationResult.Success;
            }

            return new ValidationResult("Data de nascimento inválida.");
        }
    }
}