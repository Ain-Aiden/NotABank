using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NotABank.Models;

namespace NotABank.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<NotABank.Models.BankAccount> BankAccount { get; set; }
        public DbSet<NotABank.Models.BankAccountLog> BankAccountLog { get; set; }
        public DbSet<NotABank.Models.Profile> Profile { get; set; }
        public DbSet<NotABank.Models.ProfileLog> ProfileLog { get; set; }
        public DbSet<NotABank.Models.ExchangeRate> ExchangeRate { get; set; }
    }
}
