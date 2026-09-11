using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Interface;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Application.Services
{
    public class EventoService : IEventoService
    {
        private readonly IEventoRepository _eventos;      // leitura (Dapper)
        private readonly EventFlowDbContext _context;     // escrita (EF)

        public EventoService(IEventoRepository eventos, EventFlowDbContext context)
        {
            _eventos = eventos;
            _context = context;
        }

        public async Task<Evento> CriarEventoAsync(CriarEventoRequest request, int organizadorId)
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
                Ativo = true,
                OrganizadorId = organizadorId   // vem do token, nunca do corpo
            };

            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync();

            return evento;
        }

        // Leitura pura: vai direto para JSON, ninguem altera o resultado.
        public Task<IEnumerable<Evento>> ListarEventosAsync() => _eventos.ListarAsync();

        public Task<Evento?> ObterPorIdAsync(int id) => _eventos.ObterPorIdAsync(id);

       
        public async Task<bool> InativarEventoAsync(int id, int organizadorId)
        {
            var evento = await _context.Eventos.FindAsync(id);

            // Evento de outro organizador e tratado como inexistente: dizer
            // "nao e seu" confirmaria que o id existe, e os ids sao sequenciais.
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

            // Evento de outro organizador e tratado como inexistente: dizer
            // "nao e seu" confirmaria que o id existe, e os ids sao sequenciais.
            if (evento == null || evento.OrganizadorId != organizadorId)
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
