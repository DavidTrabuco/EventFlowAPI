using EventFlow.Application.DTOs.Request;
using EventFlow.Application.DTOs.Response;
using EventFlow.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngressoController : ControllerBase
    {
        private readonly IIngressoService _ingressoService;

        public IngressoController(IIngressoService ingressoService)
        {
            _ingressoService = ingressoService;
        }

        [HttpPost("comprar")]
        public async Task<IActionResult> ComprarIngresso([FromBody] ComprarIngressoRequest request)
        {
            try
            {
                var ingresso = await _ingressoService.ComprarIngressoAsync(request);
                return Ok(IngressoResponse.De(ingresso));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/cancelar")]
        public async Task<IActionResult> CancelarIngresso(int id)
        {
            try
            {
                await _ingressoService.CancelarIngressoAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("checkin/{codigo}")]
        public async Task<IActionResult> CheckIn(string codigo)
        {
            try
            {
                await _ingressoService.ValidarCheckInAsync(codigo);
                return Ok(new { message = "Check-in realizado com sucesso!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("participante/{participanteId}")]
        public async Task<IActionResult> ListarPorParticipante(int participanteId)
        {
            var ingressos = await _ingressoService.ListarPorParticipanteAsync(participanteId);
            return Ok(ingressos.Select(IngressoResponse.De));
        }
    }
}
