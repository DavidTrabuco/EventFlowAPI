using System.ComponentModel.DataAnnotations;

namespace EventFlow.Application.DTOs.Request
{
    public class ComprarIngressoRequest
    {
        // ValorPago, DataCompra e CodigoIngresso saíram de propósito:
        // são calculados pelo servidor. O preço vem sempre do evento.
        // ParticipanteId também não entra aqui: vem do usuário autenticado,
        // senão qualquer um compraria em nome de outra pessoa.
        [Range(1, int.MaxValue, ErrorMessage = "EventoId inválido.")]
        public int EventoId { get; set; }
    }
}
