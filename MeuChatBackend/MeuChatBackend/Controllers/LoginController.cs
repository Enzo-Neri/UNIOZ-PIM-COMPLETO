using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MeuChatBackend.Models;
using System.Data;
using Microsoft.Data.SqlClient; // Usando a versão mais moderna do SqlClient
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System;

namespace MeuChatBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Rota será /api/login
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Autenticar([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.RA) || string.IsNullOrEmpty(loginRequest.Senha))
            {
                return BadRequest("RA e Senha são obrigatórios.");
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                await using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    await using (SqlCommand command = new SqlCommand("dbo.sp_LoginUsuario", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@RA", loginRequest.RA);
                        command.Parameters.AddWithValue("@Senha", loginRequest.Senha);

                        await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // --- LÓGICA DE GERAÇÃO E RETORNO DO TOKEN ---

                                // 1. Pegamos os dados do usuário que vieram do banco
                                // Certifique-se que sua Stored Procedure retorna "UsuarioID" e "Nome"
                                string usuarioId = reader["UsuarioID"].ToString();
                                string nome = reader["Nome"].ToString();

                                // 2. Usamos esses dados para gerar o token
                                string tokenGerado = GerarTokenJwt(usuarioId, nome);

                                new Claim("nome", nome);

                                // 3. Retornamos o token em um objeto JSON, como o front-end espera
                                return Ok(new { token = tokenGerado });
                            }
                            else
                            {
                                // Se não houver linhas, as credenciais estão incorretas
                                return Unauthorized("RA ou senha inválidos.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Em um sistema real, você deveria logar este erro detalhadamente
                return StatusCode(500, $"Erro interno do servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Gera um Token JWT para um usuário autenticado.
        /// </summary>
        /// <param name="usuarioId">O ID do usuário, para ser incluído no token.</param>
        /// <param name="nome">O nome do usuário, para ser incluído no token.</param>
        /// <returns>Uma string representando o Token JWT.</returns>
        private string GerarTokenJwt(string usuarioId, string nome)
        {
            // A chave secreta é pega do appsettings.json
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Claims são as "informações" que guardamos dentro do token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId), // ID do usuário
                new Claim(ClaimTypes.Name, nome)                 // Nome do usuário
            };

            // Criando o token
            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.Now.AddHours(8), // Duração do token
                signingCredentials: credentials);

            // Escrevendo o token como uma string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}