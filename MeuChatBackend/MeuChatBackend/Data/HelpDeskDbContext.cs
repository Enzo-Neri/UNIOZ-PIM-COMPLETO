using Microsoft.EntityFrameworkCore;
using MeuChatBackend.Models; // <-- GARANTA QUE ESTA LINHA ESTEJA AQUI

// Substitua "MeuChatBackend.Data" pelo namespace correto, se for diferente.
namespace MeuChatBackend.Data 
{
    // ESTE É O NOME QUE VOCÊ PROCURAVA!
    // Ele herda de DbContext, que é a base do Entity Framework.
    public class HelpDeskDbContext : DbContext
    {
        // Este construtor é essencial para a injeção de dependência no Program.cs
        public HelpDeskDbContext(DbContextOptions<HelpDeskDbContext> options) : base(options)
        {
        }

        // --- Mapeamento das suas classes de modelo para as tabelas do banco ---
        // A propriedade "Chamado" vai se conectar à tabela "dbo.Chamado"
        public DbSet<Chamado> Chamado { get; set; }

        // A propriedade "Usuario" vai se conectar à tabela "dbo.Usuario"
        public DbSet<Usuario> Usuario { get; set; }

        // Se você tivesse mais tabelas, adicionaria mais DbSets aqui.
    }
}