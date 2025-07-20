using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingInvitations.Api.Data;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Controllers
{
    [ApiController]
    [Route("api/invitation")]
    public class InvitationController : ControllerBase
    {
        private readonly WeddingDbContext _context;

        public InvitationController(WeddingDbContext context)
        {
            _context = context;
        }

        // GET: api/invitation/{code} - Obtener invitación por código
        [HttpGet("{code}")]
        public async Task<ActionResult<object>> GetInvitation(string code)
        {
            var family = await _context.Families
                .Include(f => f.Guests)
                .FirstOrDefaultAsync(f => f.InvitationCode == code);

            if (family == null)
            {
                return NotFound(new { message = "Invitación no encontrada" });
            }

            // Marcar como vista si es la primera vez
            if (!family.InvitationViewed)
            {
                family.InvitationViewed = true;
                family.ViewedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                familyName = family.FamilyName,
                contactPerson = family.ContactPerson,
                maxGuests = family.MaxGuests,
                confirmedGuests = family.ConfirmedGuests,
                hasResponded = family.Responded,
                isAttending = family.Attending,
                formCompleted = family.FormCompleted,
                responseDeadline = family.ResponseDeadline,
                guests = family.Guests,

                // Información de eventos
                events = new[]
                {
                    new {
                        name = "Ceremonia Religiosa",
                        dateTime = family.ReligiousDateTime,
                        venue = family.ReligiousVenue,
                        address = family.ReligiousAddress,
                        requiresConfirmation = false
                    },
                    new {
                        name = "Ceremonia Civil",
                        dateTime = family.CivilDateTime,
                        venue = family.CivilVenue,
                        address = family.CivilAddress,
                        requiresConfirmation = false
                    },
                    new {
                        name = "Recepción",
                        dateTime = family.ReceptionDateTime,
                        venue = family.ReceptionVenue,
                        address = family.ReceptionAddress,
                        requiresConfirmation = true
                    }
                }
            });
        }

        // POST: api/invitation/{code}/respond - Confirmar asistencia
        [HttpPost("{code}/respond")]
        public async Task<ActionResult> RespondToInvitation(string code, [FromBody] RespondRequest request)
        {
            var family = await _context.Families.FirstOrDefaultAsync(f => f.InvitationCode == code);
            if (family == null)
            {
                return NotFound(new { message = "Invitación no encontrada" });
            }

            // Verificar deadline
            if (DateTime.UtcNow > family.ResponseDeadline)
            {
                return BadRequest(new { message = "El plazo para responder ha expirado" });
            }

            // Actualizar respuesta
            family.Responded = true;
            family.ResponseDate = DateTime.UtcNow;
            family.Attending = request.Attending;
            family.UpdatedAt = DateTime.UtcNow;

            if (request.Attending)
            {
                family.Status = "pending"; // Pendiente de completar formulario
            }
            else
            {
                family.Status = "declined";
                family.FormCompleted = true; // No necesita formulario si no asiste
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Respuesta guardada exitosamente" });
        }
    }

    // Clase para el request de respuesta
    public class RespondRequest
    {
        public bool Attending { get; set; }
    }
}