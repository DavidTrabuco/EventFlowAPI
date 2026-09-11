using System.Reflection.Metadata;

namespace EventFlow.Domain.Entity
{
    public class Evento
    {

        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;

        public int OrganizadorId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public string Local { get; set; } = string.Empty;
        public int CapacidadeMaxima { get; set; }
        public int IngressosVendidos { get; set; }
        public decimal PrecoIngresso { get; set; }
        public bool Ativo { get; set; } = true;



        public Evento() { }

        public Evento(int id, string titulo, string descricao, DateTime dataHora, string local, int capacidadeMaxima, int ingressosVendidos, decimal precoIngresso, bool ativo)
        {
            Id = id;
            Titulo = titulo;
            Descricao = descricao;
            DataHora = dataHora;
            Local = local;
            CapacidadeMaxima = capacidadeMaxima;
            IngressosVendidos = ingressosVendidos;
            PrecoIngresso = precoIngresso;
            Ativo = ativo;
        }
    }
}

