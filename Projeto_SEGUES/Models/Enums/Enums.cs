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
            [Display(Name = "Externo")]
            External,
            [Display(Name = "TrabalhadorIPS")]
            IPSWorker
        }
        public enum UserStatus { Active, Inactive, Suspended }
        public enum Gender {
            [Display(Name = "Masculino")]
            Male,
            [Display(Name = "Feminino")]
            Female,
            [Display(Name = "Outro")]
            Other }
        public enum DiscountType { Percentage, Fixed }
        public enum TicketState { Available = 0, Used = 1, Expired = 2 }
        public enum TicketType
        {
            [Display(Name = "Estudante")]
            Student,

            [Display(Name = "Administrador")]
            Admin,

            [Display(Name = "Funcionário")]
            Employee,

            [Display(Name = "Externo")]
            External,

            [Display(Name = "Trabalhador IPS")]
            IPSWorker
        }
    }
}
