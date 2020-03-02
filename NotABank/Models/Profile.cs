using NotABank.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NotABank.Models
{
    public class Profile
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public string HomeAddress { get; set; }
        public string IV { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
