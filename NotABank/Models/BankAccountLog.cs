using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotABank.Models
{
    public class BankAccountLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int AccountId { get; set; }
        public string Action { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
    }
}
