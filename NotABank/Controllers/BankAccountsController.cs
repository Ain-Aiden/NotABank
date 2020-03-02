using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotABank.Data;
using NotABank.Models;

namespace NotABank.Controllers
{
    [Authorize]
    public class BankAccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private static byte[] PrivateKey;
        private UserManager<ApplicationUser> _userManager;

        public BankAccountsController(ApplicationDbContext context, IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;

            //Load saved key
            PrivateKey = Convert.FromBase64String(_configuration.GetValue<string>("AesKey"));
        }

        // GET: BankAccounts
        public async Task<IActionResult> Index()
        {
            List<BankAccount> accountsList = new List<BankAccount>();

            //Check if the logged-in user is an admin
            if (IsAdmin().Result)
            {
                accountsList = await _context.BankAccount.Include(b => b.User).ToListAsync(); //show all accounts

            }
            else
            {
                //Only show bank accounts for the logged-in user
                accountsList = await _context.BankAccount.Where(a => a.UserId == GetLoggedInUserId()).Include(b => b.User).ToListAsync();
            }

            foreach (BankAccount acc in accountsList)
            {
                //decrypt the account number
                acc.AccountNumber = DecryptAccountNumber(acc);
            }

            return View(accountsList);
            //var applicationDbContext = _context.BankAccount.Include(b => b.User);
            //return View(await applicationDbContext.ToListAsync());
        }

        // GET: BankAccounts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bankAccount = await _context.BankAccount
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bankAccount == null)
            {
                return NotFound();
            }

            //Deny access to other users accounts, unless the user is an admin
            if (bankAccount.UserId != GetLoggedInUserId() && !IsAdmin().Result)
            {
                return StatusCode(403); //Should we use status code 500 instead?
            }

            //decrypt the account number
            bankAccount.AccountNumber = DecryptAccountNumber(bankAccount);

            return View(bankAccount);
        }

        // GET: BankAccounts/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: BankAccounts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,AccountNumber,Balance")] BankAccount bankAccount)
        {
            //Set the UserId to the currently logged-in user
            bankAccount.UserId = GetLoggedInUserId();

            //Create an Aes object, using the private key
            using (Aes aesEncryptor = Aes.Create())
            {
                //Use the main private key & IV
                aesEncryptor.Key = PrivateKey;

                //Save the generated IV to the account record
                bankAccount.IV = Convert.ToBase64String(aesEncryptor.IV);


                // Create an encryptor
                var encryptor = aesEncryptor.CreateEncryptor();

                //Encrypt the account number using the shared private key, and the user specific IV
                byte[] accountNumberInBytes = Encoding.UTF8.GetBytes(bankAccount.AccountNumber);
                byte[] encryptedAccountNumberInBytes = encryptor.TransformFinalBlock(accountNumberInBytes, 0, accountNumberInBytes.Length);

                bankAccount.AccountNumber = Convert.ToBase64String(encryptedAccountNumberInBytes);


            }
            if (ModelState.IsValid)
            {
                _context.Add(bankAccount);
                await _context.SaveChangesAsync();

                ////////Log creation of a new account////////
                BankAccountLog newLogRecord = new BankAccountLog()
                {
                    UserId = _userManager.GetUserId(User),
                    AccountId = bankAccount.Id,
                    Action = "Create"
                };
                _context.Add(newLogRecord);
                await _context.SaveChangesAsync();
                //////////////////////////////////////////

                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", bankAccount.UserId);
            return View(bankAccount);
        }

        // GET: BankAccounts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bankAccount = await _context.BankAccount.FindAsync(id);
            if (bankAccount == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", bankAccount.UserId);
            return View(bankAccount);
        }

        // POST: BankAccounts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,AccountNumber,Balance")] BankAccount bankAccount)
        {
            if (id != bankAccount.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bankAccount);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BankAccountExists(bankAccount.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", bankAccount.UserId);
            return View(bankAccount);
        }

        // GET: BankAccounts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bankAccount = await _context.BankAccount
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bankAccount == null)
            {
                return NotFound();
            }

            return View(bankAccount);
        }

        // POST: BankAccounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bankAccount = await _context.BankAccount.FindAsync(id);
            _context.BankAccount.Remove(bankAccount);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BankAccountExists(int id)
        {
            return _context.BankAccount.Any(e => e.Id == id);
        }

        private string DecryptAccountNumber(BankAccount bankAccount)
        {
            //Create an Aes object, using the private key and the saved user IV
            using (Aes aesDecryptor = Aes.Create())
            {
                //Use the main private key & saved user IV
                aesDecryptor.Key = PrivateKey;
                aesDecryptor.IV = Convert.FromBase64String(bankAccount.IV);

                // Create a decryptor
                var decryptor = aesDecryptor.CreateDecryptor();

                //Decrypt the account number using the shared private key, and the account specific IV
                byte[] encryptedAccountNumberInBytes = Convert.FromBase64String(bankAccount.AccountNumber);
                byte[] decryptedAccountNumberInBytes = decryptor.TransformFinalBlock(encryptedAccountNumberInBytes, 0, encryptedAccountNumberInBytes.Length);

                return Encoding.UTF8.GetString(decryptedAccountNumberInBytes);

            }

        }

        private string GetLoggedInUserId()
        {
            return _userManager.GetUserId(User);
        }

        private async Task<bool> IsAdmin()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            return (await _userManager.GetRolesAsync(user)).ToList().Contains("Admin");
        }
    }
}
