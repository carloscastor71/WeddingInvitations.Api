using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingInvitations.Api.Data;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FamiliesController : ControllerBase
    {
        private readonly WeddingDbContext _context;

        public FamiliesController(WeddingDbContext context)
        {
            _context = context;
        }

        // GET: api/families
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Family>>> GetFamilies()
        {
            return await _context.Families.Include(f => f.Guests).ToListAsync();
        }

        // GET: api/families/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Family>> GetFamily(int id)
        {
            var family = await _context.Families
                .Include(f => f.Guests)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (family == null)
            {
                return NotFound();
            }

            return family;
        }

        // POST: api/families
        [HttpPost]
        public async Task<ActionResult<Family>> CreateFamily(Family family)
        {
            family.InvitationCode = Family.GenerateInvitationCode();
            family.CreatedAt = DateTime.UtcNow;
            family.UpdatedAt = DateTime.UtcNow;

            _context.Families.Add(family);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFamily), new { id = family.Id }, family);
        }
        // PUT: api/families/5/mark-sent
        [HttpPut("{id}/mark-sent")]
        public async Task<IActionResult> MarkAsSent(int id)
        {
            var family = await _context.Families.FindAsync(id);
            if (family == null)
            {
                return NotFound();
            }

            family.InvitationSent = true;
            family.SentDate = DateTime.UtcNow;
            family.Status = "pending";
            family.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(family);
        }

        // PUT: api/families/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFamily(int id, Family family)
        {
            if (id != family.Id)
            {
                return BadRequest();
            }

            family.UpdatedAt = DateTime.UtcNow;
            _context.Entry(family).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FamilyExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/families/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamily(int id)
        {
            var family = await _context.Families.FindAsync(id);
            if (family == null)
            {
                return NotFound();
            }

            _context.Families.Remove(family);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FamilyExists(int id)
        {
            return _context.Families.Any(e => e.Id == id);
        }
    }
}