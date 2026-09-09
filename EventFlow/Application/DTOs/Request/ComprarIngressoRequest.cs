using System.ComponentModel.DataAnnotations;

namespace EventFlow.Application.DTOs.Request
{
    public class ComprarIngressoRequest
    {
        // ValorPago, DataCompra e CodigoIngresso saíram de propósito:
        // são calculados pelo servidor. O preço vem sempre do evento.
        [Range(1, int.MaxValue, ErrorMessage = "EventoId inválido.")]
        public int EventoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ParticipanteId inválido.")]
        public int ParticipanteId { get; set; }
    }
}
