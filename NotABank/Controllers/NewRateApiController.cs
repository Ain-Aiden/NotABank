using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotABank.Data;
using NotABank.Models;

namespace NotABank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewRateApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private static byte[] PrivateKey;

        public NewRateApiController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            //Load saved key
            PrivateKey = Convert.FromBase64String(_configuration.GetValue<string>("HmacKey"));
        }

        // GET: api/NewRateApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExchangeRate>>> GetExchangeRate()
        {
            return await _context.ExchangeRate.ToListAsync();
        }

        // GET: api/NewRateApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExchangeRate>> GetExchangeRate(string id)
        {
            var exchangeRate = await _context.ExchangeRate.FindAsync(id);

            if (exchangeRate == null)
            {
                return NotFound();
            }

            return exchangeRate;
        }

        // PUT: api/NewRateApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutExchangeRate(string id, ExchangeRate exchangeRate)
        {
            if (id != exchangeRate.Id)
            {
                return BadRequest();
            }

            _context.Entry(exchangeRate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExchangeRateExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/NewRateApi
        [HttpPost]
        public async Task<ActionResult<ExchangeRate>> PostExchangeRate(ExchangeRate exchangeRate)
        {
            exchangeRate.DateAdded = DateTime.Now;
            using (HMACSHA512 hmac = new HMACSHA512(PrivateKey))
            {
                byte[] encodedRawRate = Encoding.UTF8.GetBytes(exchangeRate.Rate.ToString());
                byte[] hashedData = hmac.ComputeHash(encodedRawRate);

                exchangeRate.Signature = Convert.ToBase64String(hashedData);

            }

            _context.ExchangeRate.Add(exchangeRate);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetExchangeRate", new { id = exchangeRate.Id }, exchangeRate);
        }

        // DELETE: api/NewRateApi/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ExchangeRate>> DeleteExchangeRate(string id)
        {
            var exchangeRate = await _context.ExchangeRate.FindAsync(id);
            if (exchangeRate == null)
            {
                return NotFound();
            }

            _context.ExchangeRate.Remove(exchangeRate);
            await _context.SaveChangesAsync();

            return exchangeRate;
        }

        private bool ExchangeRateExists(string id)
        {
            return _context.ExchangeRate.Any(e => e.Id == id);
        }
    }
}
