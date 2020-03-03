using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotABank.Data;
using NotABank.Models;


namespace NotABank.Controllers
{
    public class SetupController : Controller   
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SetupController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> RoleManager)
        {
            _userManager = userManager;
            _roleManager = RoleManager;
        }

        // Print out the generated keys
        public string PrintAesKey()
        {
            /// Generate a new AES private key
            using (Aes AesGen = Aes.Create())
            {
                return Convert.ToBase64String(AesGen.Key);
            }
        }

        public string PrintAesIV()
        {
            /// Generate a new AES private IV
            using (Aes AesGen = Aes.Create())
            {
                return Convert.ToBase64String(AesGen.IV);
            }
        }
        public string PrintHmackey()
        {
            byte[] secretkey = new Byte[64];
            using (var rngProvider = new RNGCryptoServiceProvider())
            {
                rngProvider.GetBytes(secretkey);
                return Convert.ToBase64String(secretkey);
            }
        }

        //Seed the database with users, roles and assign users to roles
        public async Task<IActionResult> SeedUserData()
        {
            //Variable to hold the status of our identity operations
            IdentityResult result;

            //Create 2 new roles (Customer, Admin)
            result = await _roleManager.CreateAsync(new IdentityRole("Customer"));
            if (!result.Succeeded)
                return View("Error", new ErrorViewModel { RequestId = "Failed to add Customer role" });

            result = await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!result.Succeeded)
                return View("Error", new ErrorViewModel { RequestId = "Failed to add Admin role" });


            //Create a list of customers
            List<ApplicationUser> CustomersList = new List<ApplicationUser>();

            //Sample bank clients
            CustomersList.Add(new ApplicationUser
            {
                Email = "Customer@One.com",
                UserName = "Customer@One.com"
            });
            CustomersList.Add(new ApplicationUser
            {
                Email = "Customer@Two.com",
                UserName = "Customer@Two.com"
            });
            CustomersList.Add(new ApplicationUser
            {
                Email = "Customer@Three.com",
                UserName = "Customer@Three.com"
            });

            foreach (ApplicationUser cust in CustomersList)
            {
                //Create the new user
                result = await _userManager.CreateAsync(cust, "Mohawk1!");
                if (!result.Succeeded)
                    return View("Error", new ErrorViewModel { RequestId = "Failed to add new user" });
                //Assign the new user to the customer role
                result = await _userManager.AddToRoleAsync(cust, "Customer");
                if (!result.Succeeded)
                    return View("Error", new ErrorViewModel { RequestId = "Failed to assign customer role" });

            }

            //Create a list of admins
            List<ApplicationUser> AdminsList = new List<ApplicationUser>();

            //Sample bank admins
            AdminsList.Add(new ApplicationUser
            {
                Email = "Admin@One.com",
                UserName = "Admin@One.com"
            });
            AdminsList.Add(new ApplicationUser
            {
                Email = "Admin@Two.com",
                UserName = "Admin@Two.com"
            });


            foreach (ApplicationUser adm in AdminsList)
            {
                //Create the new user
                result = await _userManager.CreateAsync(adm, "Mohawk1!");
                if (!result.Succeeded)
                    return View("Error", new ErrorViewModel { RequestId = "Failed to add new admin user" });
                //Assign the new user to the customer role
                result = await _userManager.AddToRoleAsync(adm, "Admin");
                if (!result.Succeeded)
                    return View("Error", new ErrorViewModel { RequestId = "Failed to assign admin role" });

            }



            //If we are here, everything executed according to plan, so we will show a success message
            return Content("Users setup completed");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}