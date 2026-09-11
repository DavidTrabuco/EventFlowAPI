using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Interface;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Application.Services
{
    public class ParticipanteService : IParticipanteService
    {
        private readonly IParticipanteRepository _participantes;   // leitura (Dapper)
        private readonly EventFlowDbContext _context;              // escrita (EF)

        public ParticipanteService(IParticipanteRepository participantes,
                                   EventFlowDbContext context)
        {
            _participantes = participantes;
            _context = context;
        }

        public async Task<Participante> CriarParticipanteAsync(CriarParticipanteRequest request, int usuarioId)
        {
            var jaTemPerfil = await _participantes.UsuarioJaTemPerfilAsync(usuarioId);
            if (jaTemPerfil)
            {
                throw new InvalidOperationException("Este usuario ja possui um perfil de participante.");
            }

            var cpfEmUso = await _participantes.CpfJaExisteAsync(request.Cpf);
            if (cpfEmUso)
            {
                throw new InvalidOperationException("Já existe um participante com este CPF.");
            }

            var emailEmUso = await _participantes.EmailJaExisteAsync(request.Email);
            if (emailEmUso)
            {
                throw new InvalidOperationException("Já existe um participante com este e-mail.");
            }

            var participante = new Participante
            {
                Nome = request.Nome,
                Email = request.Email,
                Cpf = request.Cpf,
                UsuarioId = usuarioId
            };

            _context.Participantes.Add(participante);
            await _context.SaveChangesAsync();

            return participante;
        }

        public Task<IEnumerable<Participante>> ListarParticipantesAsync() =>
            _participantes.ListarAsync();

        public Task<Participante?> ObterPorIdAsync(int id) =>
            _participantes.ObterPorIdAsync(id);

        public Task<Participante?> ObterPorUsuarioIdAsync(int usuarioId) =>
            _participantes.ObterPorUsuarioIdAsync(usuarioId);
    }
}
