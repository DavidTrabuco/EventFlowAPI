using EventFlow.Application.DTOs.Request;
using EventFlow.Application.DTOs.Response;
using EventFlow.Domain.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventFlow.Api.Extension;

namespace EventFlow.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class IngressoController : ControllerBase
    {
        private readonly IIngressoService _ingressoService;
        private readonly IParticipanteService _participanteService;

        public IngressoController(IIngressoService ingressoService, IParticipanteService participanteService)
        {
            _ingressoService = ingressoService;
            _participanteService = participanteService;
        }

        // Traduz o Usuario do token no Participante correspondente.
        // Retorna null quando o usuario ainda nao criou seu perfil.
        private async Task<int?> ObterParticipanteIdAsync()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var participante = await _participanteService.ObterPorUsuarioIdAsync(usuarioId);
            return participante?.Id;
        }

        [HttpPost("comprar")]
        public async Task<IActionResult> ComprarIngresso([FromBody] ComprarIngressoRequest request)
        {
            var participanteId = await ObterParticipanteIdAsync();
            if (participanteId is null)
                return NotFound(new { message = "Voce ainda nao criou seu perfil de participante." });

            try
            {
                var ingresso = await _ingressoService.ComprarIngressoAsync(request, participanteId.Value);
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
            var participanteId = await ObterParticipanteIdAsync();
            if (participanteId is null)
                return NotFound(new { message = "Voce ainda nao criou seu perfil de participante." });

            try
            {
                await _ingressoService.CancelarIngressoAsync(id, participanteId.Value);
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

        [Authorize(Policy = Policies.ApenasOrganizador)]
        //[Authorize(Roles = "Organizador")]
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
        [Authorize(Policy = Policies.ApenasOrganizador)]
        //[Authorize(Roles = "Organizador")]
        [HttpGet("participante/{participanteId}")]
        public async Task<IActionResult> ListarPorParticipante(int participanteId)
        {
            var ingressos = await _ingressoService.ListarPorParticipanteAsync(participanteId);
            return Ok(ingressos.Select(IngressoResponse.De));
        }


        [HttpGet("meus")]
        public async Task<IActionResult> ListarMeusIngressos()
        {
            var participanteId = await ObterParticipanteIdAsync();
            if (participanteId is null)
                return NotFound(new { message = "Voce ainda nao criou seu perfil de participante." });

            var ingressos = await _ingressoService.ListarPorParticipanteAsync(participanteId.Value);
            return Ok(ingressos.Select(IngressoResponse.De));
        }

        
    }
}
