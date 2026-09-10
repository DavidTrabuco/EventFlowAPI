using EventFlow.Application.DTOs.Request;
using EventFlow.Application.DTOs.Response;
using EventFlow.Domain.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class EventoController : ControllerBase
    {
        private readonly IEventoService _eventoService;

        public EventoController(IEventoService eventoService)
        {
            _eventoService = eventoService;
        }

        [Authorize(Roles = "Organizador")]
        [HttpPost]
        public async Task<IActionResult> CriarEvento([FromBody] CriarEventoRequest request)
        {
            try
            {
                var evento = await _eventoService.CriarEventoAsync(request);
                return CreatedAtAction(
                    nameof(ObterEventoPorId),
                    new { id = evento.Id },
                    EventoResponse.De(evento));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListarEventos()
        {
            var eventos = await _eventoService.ListarEventosAsync();
            return Ok(eventos.Select(EventoResponse.De));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterEventoPorId(int id)
        {
            var evento = await _eventoService.ObterPorIdAsync(id);
            if (evento == null)
            {
                return NotFound(new { message = "Evento não encontrado." });
            }

            return Ok(EventoResponse.De(evento));
        }

        [Authorize(Roles = "Organizador")]
        [HttpPut("{id}/inativar")]
        public async Task<IActionResult> InativarEvento(int id)
        {
            try
            {
                var inativado = await _eventoService.InativarEventoAsync(id);
                return inativado
                    ? NoContent()
                    : BadRequest(new { message = "O evento já está inativo." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Organizador")]
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarEvento(int id, [FromBody] AtualizarEventoRequest request)
        {
            try
            {
                var eventoAtualizado = await _eventoService.AtualizarEventoAsync(id, request);
                return Ok(EventoResponse.De(eventoAtualizado));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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
    }
}
