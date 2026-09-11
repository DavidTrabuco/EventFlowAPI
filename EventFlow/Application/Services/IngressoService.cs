using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Enums;
using EventFlow.Domain.Interface;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Application.Services
{
    public class IngressoService : IIngressoService
    {
        private const int LimiteIngressosPorParticipante = 5;

        private readonly IIngressoRepository _ingressos;    // leitura (Dapper)
        private readonly IParticipanteRepository _participantes;
        private readonly EventFlowDbContext _context;       // escrita (EF)

        public IngressoService(IIngressoRepository ingressos,
                               IParticipanteRepository participantes,
                               EventFlowDbContext context)
        {
            _ingressos = ingressos;
            _participantes = participantes;
            _context = context;
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

            // RN06 antes de RN01: se o evento está inativo, essa é a informação útil.
            if (!evento.Ativo)
            {
                throw new InvalidOperationException("RN06: evento inativo, compras bloqueadas.");
            }

            // RN01
            if (evento.IngressosVendidos >= evento.CapacidadeMaxima)
            {
                throw new InvalidOperationException("RN01: capacidade máxima do evento atingida.");
            }

            // RN03: apenas ingressos ATIVOS contam. Cancelado libera a vaga do limite.
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
                ValorPago = evento.PrecoIngresso,                    // preço vem do evento
                CodigoValidacao = await GerarCodigoUnicoAsync(),     // RN05
                Status = StatusIngresso.Ativo
            };

            _context.Ingressos.Add(ingresso);
            evento.IngressosVendidos++;   // evento está rastreado: SaveChanges persiste os dois

            await _context.SaveChangesAsync();

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

            // Um ingresso de outro participante e tratado como inexistente:
            // responder "nao e seu" confirmaria que o id existe.
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

            // RN04: 24h de antecedência. Ambos os lados em UTC (ver EventoService).
            if ((ingresso.Evento.DataHora - DateTime.UtcNow).TotalHours < 24)
            {
                throw new InvalidOperationException(
                    "RN04: cancelamento permitido apenas com 24h de antecedência.");
            }

            ingresso.Status = StatusIngresso.Cancelado;
            ingresso.Evento.IngressosVendidos--;   // devolve a vaga

            await _context.SaveChangesAsync();
            return true;
        }

        // RN05: check-in marca o ingresso como utilizado.
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
            return true;
        }

        // RF04: histórico do participante.
        public Task<IEnumerable<Ingresso>> ListarPorParticipanteAsync(int participanteId) =>
            _ingressos.ListarPorParticipanteAsync(participanteId);

        // RN05: código de 8 caracteres, único.
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
