using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Interface;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;  


namespace EventFlow.Application.Services
{
    public class ParticipanteService : IParticipanteService
    {
        private readonly IParticipanteRepository _participantes;   
        private readonly EventFlowDbContext _context;      
        
        private readonly IMemoryCache _cache;

       

        public ParticipanteService(IParticipanteRepository participantes,EventFlowDbContext context, IMemoryCache cache)
        {
            _participantes = participantes;
            _context = context;
            _cache = cache;
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
            _cache.Remove("participantes");

            return participante;
        }

        public async Task<IEnumerable<Participante>> ListarParticipantesAsync()
        {
            if (_cache.TryGetValue("participantes", out IEnumerable<Participante>? participantes))
            {
                return participantes!;
            }

            participantes = await _participantes.ListarAsync();
            _cache.Set("participantes", participantes, TimeSpan.FromMinutes(5));
            return participantes;
        }

        public async  Task<Participante?> ObterPorIdAsync(int id)
        {
            if (_cache.TryGetValue($"participante_{id}", out Participante? participante))
            {
                return participante;    
            }

            var participanteDb = await _participantes.ObterPorIdAsync(id);
            if (participanteDb != null)
            {
                _cache.Set($"participante_{id}", participanteDb, TimeSpan.FromMinutes(5));
            }
            return participanteDb;
        }

        public async Task<Participante?> ObterPorUsuarioIdAsync(int usuarioId)
        {
            if (_cache.TryGetValue($"participante_usuario_{usuarioId}", out Participante? participante))
            {
                return participante;
            }

            var participanteDb = await _participantes.ObterPorUsuarioIdAsync(usuarioId);
            if (participanteDb != null)
            {
                _cache.Set($"participante_usuario_{usuarioId}", participanteDb, TimeSpan.FromMinutes(5));
            }
            return participanteDb;
        }
    }
}
