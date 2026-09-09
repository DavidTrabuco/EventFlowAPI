using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Interface;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Application.Services
{
    public class ParticipanteService : IParticipanteService
    {
        private readonly EventFlowDbContext _context;

        public ParticipanteService(EventFlowDbContext context)
        {
            _context = context;
        }

        public async Task<Participante> CriarParticipanteAsync(CriarParticipanteRequest request)
        {
            // O provider InMemory NÃO aplica o índice único de CPF declarado no
            // DbContext. Enquanto não houver banco real, a unicidade é garantida aqui.
            var cpfEmUso = await _context.Participantes.AnyAsync(p => p.Cpf == request.Cpf);
            if (cpfEmUso)
            {
                throw new InvalidOperationException("Já existe um participante com este CPF.");
            }

            var emailEmUso = await _context.Participantes.AnyAsync(p => p.Email == request.Email);
            if (emailEmUso)
            {
                throw new InvalidOperationException("Já existe um participante com este e-mail.");
            }

            var participante = new Participante
            {
                Nome = request.Nome,
                Email = request.Email,
                Cpf = request.Cpf
            };

            _context.Participantes.Add(participante);
            await _context.SaveChangesAsync();

            return participante;
        }

        public async Task<IEnumerable<Participante>> ListarParticipantesAsync() =>
            await _context.Participantes.AsNoTracking().ToListAsync();

        public async Task<Participante?> ObterPorIdAsync(int id) =>
            await _context.Participantes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }
}
