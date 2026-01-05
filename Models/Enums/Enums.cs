using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums
{
    public class Enums
    {
        public enum UserRole {
            [Display(Name = "Estudante")]
            Student,
            [Display(Name = "Administrador")]
            Admin,
            [Display(Name = "Funcionário")]
            Employee,
            [Display(Name = "Funcionário Externo")]
            ExternalEmployee,
            [Display(Name = "Professor")]
            Teacher }
        public enum UserStatus { Active, Inactive, Suspended }
        public enum Gender {
            [Display(Name = "Masculino")]
            Male,
            [Display(Name = "Feminino")]
            Female,
            [Display(Name = "Outro")]
            Other }
        public enum DiscountType { Percentage, Fixed }
        public enum TicketState { Valid, Used, Expired }
        public enum TicketType { Student, Admin, Employee , ExternalEmployee }
    }
}
