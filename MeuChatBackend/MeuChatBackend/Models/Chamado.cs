// Dentro do arquivo Models/Chamado.cs

namespace MeuChatBackend.Models
{
    // Esta classe representa a tabela dbo.Chamado
    public class Chamado
    {
        public int ChamadoID { get; set; }
        public string? NumeroChamado { get; set; }
        public string Assunto { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime UltimaAtualizacao { get; set; }
        public int UsuarioID { get; set; }
    }

    // Esta é a classe que o front-end envia para criar um chamado.
    // Ela não precisa de todos os campos da tabela.
    public class ChamadoDto
    {
        public string Assunto { get; set; } = string.Empty;
        public string? Descricao { get; set; }
    }
}