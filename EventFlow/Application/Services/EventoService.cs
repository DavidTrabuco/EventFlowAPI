using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Interface;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EventFlow.Application.Services
{
    public class EventoService : IEventoService
    {
        private readonly IEventoRepository _eventos;
        private readonly EventFlowDbContext _context;

        private readonly IMemoryCache _cache;



        public EventoService(IEventoRepository eventos, EventFlowDbContext context, IMemoryCache cache)
        {
            _eventos = eventos;
            _context = context;
            _cache = cache;
        }

        public async Task<Evento> CriarEventoAsync(CriarEventoRequest request, int organizadorId)
        {

            var dataHoraUtc = request.DataHora.Kind == DateTimeKind.Utc
                ? request.DataHora
                : request.DataHora.ToUniversalTime();


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
                Ativo = true,
                OrganizadorId = organizadorId
            };

            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync();

            return evento;
        }


        public async Task<IEnumerable<Evento>> ListarEventosAsync()
        {

            if (_cache.TryGetValue("Eventos", out IEnumerable<Evento>? eventosMemoryCache))
            {
                return eventosMemoryCache!;
            }
            var evento = await _eventos.ListarAsync();

            if (evento != null)
            {
                _cache.Set("Eventos", evento, TimeSpan.FromMinutes(10));
            }
            return evento!;
        }




        public async Task<Evento?> ObterPorIdAsync(int id)
        {


            if (_cache.TryGetValue($"Evento_{id}", out Evento? EventoMemoryCache))
            {
                return EventoMemoryCache;
            }
            var evento = await _eventos.ObterPorIdAsync(id);
            if (evento != null)
            {
                _cache.Set($"Evento_{id}", evento, TimeSpan.FromMinutes(10));
            }

            return evento;
        }



        public async Task<bool> InativarEventoAsync(int id, int organizadorId)
        {
            var evento = await _context.Eventos.FindAsync(id);


            if (evento == null || evento.OrganizadorId != organizadorId)
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

        public async Task<Evento> AtualizarEventoAsync(int id, AtualizarEventoRequest request, int organizadorId)
        {
            var evento = await _context.Eventos.FindAsync(id);


            if (evento == null || evento.OrganizadorId != organizadorId)
            {
                throw new KeyNotFoundException($"Evento {id} não encontrado.");
            }

            if (request.DataHora.HasValue)
            {
                var dataHoraUtc = request.DataHora.Value.Kind == DateTimeKind.Utc
                    ? request.DataHora.Value
                    : request.DataHora.Value.ToUniversalTime();

                if (dataHoraUtc <= DateTime.UtcNow)
                {
                    throw new ArgumentException("RN02: a data do evento deve ser no futuro.");
                }

                evento.DataHora = dataHoraUtc;
            }

            if (request.CapacidadeMaxima < evento.IngressosVendidos)
            {
                throw new InvalidOperationException(
                    $"A capacidade não pode ser menor que os {evento.IngressosVendidos} ingressos já vendidos.");
            }

            evento.Titulo = request.Titulo;
            evento.Descricao = request.Descricao;
            evento.Local = request.Local;
            evento.CapacidadeMaxima = request.CapacidadeMaxima;
            evento.PrecoIngresso = request.PrecoIngresso;

            await _context.SaveChangesAsync();
            return evento;
        }


        public async Task DesativarEventosPassados()
        {

            var eventosPassados = await _context.Eventos
                .Where(e => e.DataHora < DateTime.UtcNow && e.Ativo)
                .ToListAsync();

            foreach (var evento in eventosPassados)
            {
                evento.Ativo = false;
            }

          await  _context.SaveChangesAsync();
        }
    }
}
