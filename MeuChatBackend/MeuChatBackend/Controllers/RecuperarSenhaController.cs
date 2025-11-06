using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MeuChatBackend.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

namespace MeuChatBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecuperarSenhaController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RecuperarSenhaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // --- DTO para o Passo 1 ---
        public class ValidarRequest
        {
            public string RA { get; set; }
            public string Email { get; set; }
            public string Telefone { get; set; }
            public string Token { get; set; } // O token que o usuário digitou
        }

        // --- DTO para o Passo 2 ---
        public class RedefinirRequest
        {
            public string NovaSenha { get; set; }
            public string ConfirmarSenha { get; set; }
        }

        // --- ROTA DO PASSO 1: Validar os dados e dar o "Passe Livre" ---
        [HttpPost("validar")]
        public async Task<IActionResult> ValidarDados([FromBody] ValidarRequest request)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string usuarioId = null;
            string nome = null;

            try
            {
                // --- ETAPA ÚNICA: Chamar a procedure e ler o resultado ---
                await using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    await using (SqlCommand command = new SqlCommand("[dbo].[sp_RecuperarSenha]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@RA", request.RA);
                        command.Parameters.AddWithValue("@Email", request.Email);
                        command.Parameters.AddWithValue("@Telefone", request.Telefone);
                        command.Parameters.AddWithValue("@Token", request.Token);

                        // MUDANÇA: Não é mais ExecuteScalar. Agora lemos o resultado.
                        await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            // Se o reader.ReadAsync() for 'true', a procedure encontrou o usuário
                            // e retornou a linha com ID e Nome.
                            if (await reader.ReadAsync())
                            {
                                usuarioId = reader["UsuarioID"].ToString();
                                nome = reader["Nome"].ToString();
                            }
                        }
                    }
                }

                // --- ETAPA B: Avaliar o resultado ---
                // Se o usuarioId NÃO for nulo, a procedure funcionou
                if (!string.IsNullOrEmpty(usuarioId) && !string.IsNullOrEmpty(nome))
                {
                    // Encontramos os dados, geramos o "Passe Livre"
                    string tokenDeRedefinicao = GerarTokenJwtTemporario(usuarioId, nome, 10);
                    return Ok(new { token = tokenDeRedefinicao });
                }
                else
                {
                    // A procedure não retornou linhas, então os dados estão errados.
                    return BadRequest("Os dados informados (RA, E-mail, Telefone ou Token) não conferem.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = "Erro interno do servidor.", detalhe = ex.Message });
            }
        }

        // --- ROTA DO PASSO 2: Redefinir a senha (usando o "Passe Livre") ---
        [HttpPost("redefinir")]
        [Authorize] // SÓ pode ser acessado por quem tem um "Passe Livre" válido
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirRequest request)
        {
            if (request.NovaSenha != request.ConfirmarSenha)
            {
                return BadRequest("As senhas não conferem.");
            }

            // Pega o ID do usuário de dentro do "Passe Livre" (JWT)
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Unauthorized();
            }

            // --- ESTA PARTE MUDOU ---
            // Em vez de SQL solto, chamamos a nova procedure
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                await using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    await using (SqlCommand command = new SqlCommand("[dbo].[sp_AlterarSenha]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UsuarioID", Convert.ToInt32(usuarioId));
                        command.Parameters.AddWithValue("@NovaSenha", request.NovaSenha); // Você deveria fazer HASH aqui

                        // Vamos ler a mensagem de retorno da procedure
                        var resultado = await command.ExecuteScalarAsync();

                        if (resultado != null && resultado.ToString() == "Senha alterada com sucesso!")
                        {
                            return Ok("Senha redefinida com sucesso!");
                        }
                        else
                        {
                            return BadRequest(resultado?.ToString() ?? "Não foi possível alterar a senha.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = "Erro interno ao redefinir a senha.", detalhe = ex.Message });
            }
        }

        // --- Gerador de "Passe Livre" ---
        private string GerarTokenJwtTemporario(string usuarioId, string nome, int minutosParaExpirar)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("Chave JWT não configurada.");
            }
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId),
                new Claim(ClaimTypes.Name, nome)
            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.Now.AddMinutes(minutosParaExpirar), // <-- VALIDADE CURTA
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}