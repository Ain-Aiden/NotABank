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
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private static byte[] PrivateKey;
        private UserManager<ApplicationUser> _userManager;

        public ProfilesController(ApplicationDbContext context, IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;

            //Load saved key
            PrivateKey = Convert.FromBase64String(_configuration.GetValue<string>("AesKey"));
        }

        // GET: Profiles
        public async Task<IActionResult> Index()
        {
            List<Profile> profileList = new List<Profile>();

            //Check if the logged-in user is an admin
            if (IsAdmin().Result)
            {
                profileList = await _context.Profile.Include(b => b.User).ToListAsync(); //show all accounts

            }
            else
            {
                //Only show bank accounts for the logged-in user
                profileList = await _context.Profile.Where(a => a.UserId == GetLoggedInUserId()).Include(b => b.User).ToListAsync();
            }

            //foreach (Profile profile in profileList)
            //{
            //    //decrypt the account number
            //    profile.HomeAddress = DecryptHomeAddress(profile);
            //}

            var applicationDbContext = _context.Profile.Include(p => p.User);
            return View(profileList);

            //var applicationDbContext = _context.Profile.Include(p => p.User);
            //return View(await applicationDbContext.ToListAsync());
        }

        // GET: Profiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profile
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profile == null)
            {
                return NotFound();
            }

            //Deny access to other users accounts, unless the user is an admin
            if (profile.UserId != GetLoggedInUserId() && !IsAdmin().Result)
            {
                return StatusCode(403); //Should we use status code 500 instead?
            }

            //decrypt the account number
            profile.HomeAddress = DecryptHomeAddress(profile);

            return View(profile);
        }

        // GET: Profiles/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Profiles/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,HomeAddress,IV")] Profile profile)
        {
            //Set the UserId to the currently logged-in user
            profile.UserId = GetLoggedInUserId();

            //Create an Aes object, using the private key
            using (Aes aesEncryptor = Aes.Create())
            {
                //Use the main private key & IV
                aesEncryptor.Key = PrivateKey;

                //Save the generated IV to the account record
                profile.IV = Convert.ToBase64String(aesEncryptor.IV);


                // Create an encryptor
                var encryptor = aesEncryptor.CreateEncryptor();

                //Encrypt the phone using the shared private key, and the user specific IV
                byte[] homeAddressInBytes = Encoding.UTF8.GetBytes(profile.HomeAddress);
                byte[] encryptedHomeAdressInBytes = encryptor.TransformFinalBlock(homeAddressInBytes, 0, homeAddressInBytes.Length);

                profile.HomeAddress = Convert.ToBase64String(encryptedHomeAdressInBytes);


            }
            if (ModelState.IsValid)
            {
                _context.Add(profile);
                await _context.SaveChangesAsync();

                ProfileLog newLogRecord = new ProfileLog()
                {
                    UserId = _userManager.GetUserId(User),
                    AccountId = profile.Id,
                    Action = "Create"
                };
                _context.Add(newLogRecord);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", profile.UserId);
            return View(profile);
        }

        // GET: Profiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profile.FindAsync(id);
            //Decrypt the account number
            profile.HomeAddress = DecryptHomeAddress(profile);
            if (profile == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", profile.UserId);
            return View(profile);
        }

        // POST: Profiles/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,HomeAddress,IV")] Profile profile)
        {
            if (id != profile.Id)
            {
                return NotFound();
            }

            //Encrypt the account number
            //Create an Aes object, using the private key
            using (Aes aesEncryptor = Aes.Create())
            {
                //Use the main private key & IV
                aesEncryptor.Key = PrivateKey;

                //Save the new generated IV to the account record
                profile.IV = Convert.ToBase64String(aesEncryptor.IV);

                //// Use the exists IV from the user
                //aesEncryptor.IV = Convert.FromBase64String(profile.IV);

                // Create an encryptor
                var encryptor = aesEncryptor.CreateEncryptor();

                //Encrypt the homeaddress using the shared private key, and the user specific IV
                byte[] homeAddressInBytes = Encoding.UTF8.GetBytes(profile.HomeAddress);
                byte[] encryptedHomeAdressInBytes = encryptor.TransformFinalBlock(homeAddressInBytes, 0, homeAddressInBytes.Length);

                profile.HomeAddress = Convert.ToBase64String(encryptedHomeAdressInBytes);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(profile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProfileExists(profile.Id))
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
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", profile.UserId);

            return View(profile);
        }

        // GET: Profiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profile
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        // POST: Profiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profile = await _context.Profile.FindAsync(id);
            _context.Profile.Remove(profile);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProfileExists(int id)
        {
            return _context.Profile.Any(e => e.Id == id);
        }

        private string DecryptHomeAddress(Profile profile)
        {
            //Create an Aes object, using the private key and the saved user IV
            using (Aes aesDecryptor = Aes.Create())
            {
                //Use the main private key & saved user IV
                aesDecryptor.Key = PrivateKey;
                aesDecryptor.IV = Convert.FromBase64String(profile.IV);

                // Create a decryptor
                var decryptor = aesDecryptor.CreateDecryptor();

                //Decrypt the address using the shared private key, and the account specific IV
                byte[] encryptedHomeAddressInBytes = Convert.FromBase64String(profile.HomeAddress);
                byte[] decryptedHomeAddressInBytes = decryptor.TransformFinalBlock(encryptedHomeAddressInBytes, 0, encryptedHomeAddressInBytes.Length);

                return Encoding.UTF8.GetString(decryptedHomeAddressInBytes);

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
