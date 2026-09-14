using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Enums;
using EventFlow.Domain.Interface;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EventFlow.Application.Services
{
    public class IngressoService : IIngressoService
    {
        private const int LimiteIngressosPorParticipante = 5;

        private readonly IIngressoRepository _ingressos;   
        private readonly IParticipanteRepository _participantes;
        private readonly EventFlowDbContext _context; 
        
        private readonly IMemoryCache _cache;

        public IngressoService(IIngressoRepository ingressos,IParticipanteRepository participantes,EventFlowDbContext context, IMemoryCache cache)
        {
            _ingressos = ingressos;
            _participantes = participantes;
            _context = context;
            _cache = cache;
        }

        public async Task<Ingresso> ComprarIngressoAsync(ComprarIngressoRequest request, int participanteId)
        {
            var evento = await _context.Eventos.FindAsync(request.EventoId);
            if (evento == null)
            {
                throw new KeyNotFoundException("Evento não encontrado.");
            }

            var participanteExiste = await _participantes.ObterPorIdAsync(participanteId) is not null;

            if (!participanteExiste)
            {
                throw new KeyNotFoundException("Participante não encontrado.");
            }

           
            if (!evento.Ativo)
            {
                throw new InvalidOperationException("RN06: evento inativo, compras bloqueadas.");
            }

           
            if (evento.IngressosVendidos >= evento.CapacidadeMaxima)
            {
                throw new InvalidOperationException("RN01: capacidade máxima do evento atingida.");
            }

            var ativosDoParticipante =
                await _ingressos.ContarAtivosAsync(request.EventoId, participanteId);

            if (ativosDoParticipante >= LimiteIngressosPorParticipante)
            {
                throw new InvalidOperationException(
                    $"RN03: limite de {LimiteIngressosPorParticipante} ingressos por participante.");
            }

            var ingresso = new Ingresso
            {
                EventoId = request.EventoId,
                ParticipanteId = participanteId,
                DataHoraCompra = DateTime.UtcNow,
                ValorPago = evento.PrecoIngresso,                 
                CodigoValidacao = await GerarCodigoUnicoAsync(),     
                Status = StatusIngresso.Ativo
            };

            _context.Ingressos.Add(ingresso);
            evento.IngressosVendidos++;

            await _context.SaveChangesAsync();

            _cache.Remove($"ingressos_participante_{participanteId}");

            return ingresso;
        }

        public async Task<bool> CancelarIngressoAsync(int ingressoId, int participanteId)
        {
            var ingresso = await _context.Ingressos
                .Include(i => i.Evento)
                .FirstOrDefaultAsync(i => i.Id == ingressoId);

            if (ingresso == null)
            {
                throw new KeyNotFoundException("Ingresso não encontrado.");
            }

           
            if (ingresso.ParticipanteId != participanteId)
            {
                throw new KeyNotFoundException("Ingresso não encontrado.");
            }

            if (ingresso.Status != StatusIngresso.Ativo)
            {
                throw new InvalidOperationException("Apenas ingressos ativos podem ser cancelados.");
            }

            if (ingresso.Evento == null)
            {
                throw new KeyNotFoundException("Evento do ingresso não encontrado.");
            }

       
            if ((ingresso.Evento.DataHora - DateTime.UtcNow).TotalHours < 24)
            {
                throw new InvalidOperationException(
                    "RN04: cancelamento permitido apenas com 24h de antecedência.");
            }

            ingresso.Status = StatusIngresso.Cancelado;
            ingresso.Evento.IngressosVendidos--;

            await _context.SaveChangesAsync();

            _cache.Remove($"ingressos_participante_{participanteId}");
            return true;
        }

     
        public async Task<bool> ValidarCheckInAsync(string codigoValidacao)
        {
            var ingresso = await _context.Ingressos
                .FirstOrDefaultAsync(i => i.CodigoValidacao == codigoValidacao);

            if (ingresso == null)
            {
                throw new KeyNotFoundException("Código de validação inválido.");
            }

            if (ingresso.Status != StatusIngresso.Ativo)
            {
                throw new InvalidOperationException("RN05: ingresso não está ativo para check-in.");
            }

            ingresso.Status = StatusIngresso.Utilizado;
            await _context.SaveChangesAsync();
            _cache.Remove($"ingressos_participante_{ingresso.ParticipanteId}");
            return true;
        }


        public async Task<IEnumerable<Ingresso>> ListarPorParticipanteAsync(int participanteId)
        {
            if (_cache.TryGetValue($"ingressos_participante_{participanteId}", out IEnumerable<Ingresso>? ingressos))
            {
                return ingressos!;
            }

            var participanteExiste = await _participantes.ObterPorIdAsync(participanteId) is not null;

            var ingressosList = await _ingressos.ListarPorParticipanteAsync(participanteId);

            if (participanteExiste)
            {
                _cache.Set($"ingressos_participante_{participanteId}", ingressosList, TimeSpan.FromMinutes(5));
            }

            return ingressosList;
        }



        private async Task<string> GerarCodigoUnicoAsync()
        {
            string codigo;
            bool jaExiste;

            do
            {
                codigo = Guid.NewGuid().ToString("N")[..8].ToUpper();
                jaExiste = await _ingressos.CodigoJaExisteAsync(codigo);
            }
            while (jaExiste);

            return codigo;
        }
    }
}
