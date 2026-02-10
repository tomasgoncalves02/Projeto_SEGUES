using System;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Purchase
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string PhoneNumber { get; set; }
        public string Reference { get; set; }
        public bool IsPaid { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}