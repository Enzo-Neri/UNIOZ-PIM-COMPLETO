// Dentro do arquivo Models/Usuario.cs

namespace MeuChatBackend.Models
{
    public class Usuario
    {
        public int UsuarioID { get; set; }
        public string? RA { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string Senha { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public bool? Ativo { get; set; }
        public string? Token { get; set; }
    }
}