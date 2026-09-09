using EventFlow.Application.DTOs.Request;
using EventFlow.Application.DTOs.Response;
using EventFlow.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParticipanteController : ControllerBase
    {
        private readonly IParticipanteService _participanteService;

        public ParticipanteController(IParticipanteService participanteService)
        {
            _participanteService = participanteService;
        }

        [HttpPost]
        public async Task<IActionResult> CriarParticipante([FromBody] CriarParticipanteRequest request)
        {
            try
            {
                var participante = await _participanteService.CriarParticipanteAsync(request);
                return CreatedAtAction(
                    nameof(ObterParticipantePorId),
                    new { id = participante.Id },
                    ParticipanteResponse.De(participante));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListarParticipantes()
        {
            var participantes = await _participanteService.ListarParticipantesAsync();
            return Ok(participantes.Select(ParticipanteResponse.De));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterParticipantePorId(int id)
        {
            var participante = await _participanteService.ObterPorIdAsync(id);
            if (participante == null)
            {
                return NotFound(new { message = "Participante não encontrado." });
            }

            return Ok(ParticipanteResponse.De(participante));
        }
    }
}
