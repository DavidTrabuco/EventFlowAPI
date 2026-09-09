using EventFlow.Domain.Entity;
using EventFlow.Domain.Enums;

namespace EventFlow.Application.DTOs.Response
{
    public class IngressoResponse
    {
        public int Id { get; set; }
        public int EventoId { get; set; }
        public int ParticipanteId { get; set; }
        public DateTime DataHoraCompra { get; set; }
        public decimal ValorPago { get; set; }
        public string CodigoValidacao { get; set; } = string.Empty;
        public StatusIngresso Status { get; set; }

        public static IngressoResponse De(Ingresso ingresso) => new()
        {
            Id = ingresso.Id,
            EventoId = ingresso.EventoId,
            ParticipanteId = ingresso.ParticipanteId,
            DataHoraCompra = ingresso.DataHoraCompra,
            ValorPago = ingresso.ValorPago,
            CodigoValidacao = ingresso.CodigoValidacao,
            Status = ingresso.Status
        };
    }
}
