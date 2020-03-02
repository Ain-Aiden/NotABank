using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using NotABank.Data;

namespace NotABank.Models
{
    public class BankAccount
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public string IV { get; set; }
        public virtual ApplicationUser User { get; set; }
}
}
