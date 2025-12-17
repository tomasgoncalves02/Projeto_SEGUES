using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_SEGUES.Models
{
    public class ExternalEmployee : User
    {

       
        public int SchoolId { get; set; }
        [ForeignKey("SchoolId")]
        public School? School { get; set; }
        public string InstitutionRole { get; set; }


    }
}
