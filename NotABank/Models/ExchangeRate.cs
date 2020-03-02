using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NotABank.Models
{
    public class ExchangeRate
    {
        [Key]
        public string Id { get; set; }
        public Decimal Rate { get; set; }
        public string Signature { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
