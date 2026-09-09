using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Interface;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Application.Services
{
    public class EventoService : IEventoService
    {
        private readonly EventFlowDbContext _context;

        public EventoService(EventFlowDbContext context)
        {
            _context = context;
        }

        public async Task<Evento> CriarEventoAsync(CriarEventoRequest request)
        {
           
            var dataHoraUtc = request.DataHora.Kind == DateTimeKind.Utc
                ? request.DataHora
                : request.DataHora.ToUniversalTime();

            // RN02: data estritamente no futuro.
            if (dataHoraUtc <= DateTime.UtcNow)
            {
                throw new ArgumentException("RN02: a data do evento deve ser no futuro.");
            }

            var evento = new Evento
            {
                Titulo = request.Titulo,
                Descricao = request.Descricao,
                DataHora = dataHoraUtc,
                Local = request.Local,
                CapacidadeMaxima = request.CapacidadeMaxima,
                PrecoIngresso = request.PrecoIngresso,
                IngressosVendidos = 0,
                Ativo = true
            };

            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync();

            return evento;
        }

        public async Task<IEnumerable<Evento>> ListarEventosAsync() =>
            await _context.Eventos.AsNoTracking().ToListAsync();

        public async Task<Evento?> ObterPorIdAsync(int id) =>
            await _context.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

       
        public async Task<bool> InativarEventoAsync(int id)
        {
            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null)
            {
                throw new KeyNotFoundException($"Evento {id} não encontrado.");
            }

            if (!evento.Ativo)
            {
                return false;
            }

            evento.Ativo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Evento> AtualizarEventoAsync(int id, AtualizarEventoRequest request)
        {
            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null)
            {
                throw new KeyNotFoundException($"Evento {id} não encontrado.");
            }

            var dataHoraUtc = request.DataHora.Kind == DateTimeKind.Utc
                ? request.DataHora
                : request.DataHora.ToUniversalTime();

            // RN02: data estritamente no futuro.
            if (dataHoraUtc <= DateTime.UtcNow)
            {
                throw new ArgumentException("RN02: a data do evento deve ser no futuro.");
            }

            // A capacidade não pode ficar abaixo do que já foi vendido.
            if (request.CapacidadeMaxima < evento.IngressosVendidos)
            {
                throw new InvalidOperationException(
                    $"A capacidade não pode ser menor que os {evento.IngressosVendidos} ingressos já vendidos.");
            }

            evento.Titulo = request.Titulo;
            evento.Descricao = request.Descricao;
            evento.DataHora = dataHoraUtc;
            evento.Local = request.Local;
            evento.CapacidadeMaxima = request.CapacidadeMaxima;
            evento.PrecoIngresso = request.PrecoIngresso;

            await _context.SaveChangesAsync();
            return evento;
        }
    }
}
