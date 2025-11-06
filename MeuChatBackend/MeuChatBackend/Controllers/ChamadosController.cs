using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeuChatBackend.Data;
using MeuChatBackend.Models;
using System.Security.Claims;
using System.Collections.Generic;
using System;

namespace MeuChatBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/chamados")]
    public class ChamadosController : ControllerBase
    {
        private readonly HelpDeskDbContext _context;

        public ChamadosController(HelpDeskDbContext context)
        {
            _context = context;
        }

        // --- MÉTODO AUXILIAR PARA LIMPAR O CÓDIGO ---
        private (int? UsuarioId, IActionResult ErrorResult) GetAndValidateUserId()
        {
            var usuarioIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioIdString))
            {
                return (null, Unauthorized(new { erro = "Token não contém ID do usuário." }));
            }

            if (!int.TryParse(usuarioIdString, out int usuarioId))
            {
                return (null, BadRequest(new { erro = "Formato de ID de usuário inválido no token." }));
            }

            return (usuarioId, null);
        }
        // --- FIM DO MÉTODO AUXILIAR ---


        // Rota: POST para api/chamados
        [HttpPost] // <-- Removi a linha duplicada
        public async Task<IActionResult> CriarChamado([FromBody] ChamadoDto novoChamadoDto)
        {
            try
            {
                var (usuarioId, errorResult) = GetAndValidateUserId();
                if (errorResult != null)
                {
                    return errorResult;
                }

                // --- LÓGICA DE LIMITE DE 5 CHAMADOS ---
                const int MAXIMO_CHAMADOS = 5;

                var chamadosAtuaisDoUsuario = _context.Chamado
                    .Where(c => c.UsuarioID == usuarioId.Value);

                int contagemAtual = await chamadosAtuaisDoUsuario.CountAsync();

                if (contagemAtual >= MAXIMO_CHAMADOS)
                {
                    var chamadoMaisAntigo = await chamadosAtuaisDoUsuario
                        .OrderBy(c => c.DataCriacao) 
                        .FirstOrDefaultAsync(); 

                    if (chamadoMaisAntigo != null)
                    {
                        _context.Chamado.Remove(chamadoMaisAntigo);
                        Console.WriteLine($"[INFO] Limite de {MAXIMO_CHAMADOS} atingido. Deletando chamado antigo #{chamadoMaisAntigo.ChamadoID} do usuário #{usuarioId}.");
                    }
                }
                // --- FIM DA LÓGICA DE LIMITE ---

                Console.WriteLine($"[DEBUG] CriarChamado - NameIdentifier pego do token: '{usuarioId}'");
                var chamadoParaSalvar = new Chamado
                {
                    Assunto = novoChamadoDto.Assunto,
                    Descricao = novoChamadoDto.Descricao ?? "Chamado aberto via chatbot.",
                    Status = "Aberto", 
                    DataCriacao = DateTime.Now,
                    UltimaAtualizacao = DateTime.Now,
                    UsuarioID = usuarioId.Value, 
                    NumeroChamado = $"CHAT-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}"
                };

                _context.Chamado.Add(chamadoParaSalvar);
                await _context.SaveChangesAsync();
                return Ok(chamadoParaSalvar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!!!!!!!!! ERRO AO CRIAR CHAMADO !!!!!!!!!\n{ex}");
                return StatusCode(500, new { erro = "Erro interno ao criar chamado.", detalhe = ex.Message });
            }
        }

        // --- MÉTODO QUE FALTAVA ---
        // Rota: GET para api/chamados/meus-chamados
        // Rota: GET para api/chamados/meus-chamados
        [HttpGet("meus-chamados")]
        public async Task<IActionResult> GetMeusChamados()
        {
            // --- ADICIONE ESTA LINHA ---
            Console.WriteLine("\n >>>>>> ESTOU NO 'GetMeusChamados' CORRETO (O QUE SÓ LÊ) <<<<<< \n");
            // --- FIM DA LINHA ADICIONADA ---

            try
            {
                var (usuarioId, errorResult) = GetAndValidateUserId();
                // ... resto do código ...
                if (errorResult != null)
                {
                    return errorResult;
                }

                var chamadosDoUsuario = await _context.Chamado
                                                   .Where(c => c.UsuarioID == usuarioId.Value)
                                                   .OrderByDescending(c => c.DataCriacao)
                                                   .ToListAsync();

                // Este método SÓ LÊ. Ele não deleta nada.
                return Ok(chamadosDoUsuario);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!!!!!!!!! ERRO AO BUSCAR CHAMADOS !!!!!!!!!\n{ex}");
                return StatusCode(500, new { erro = "Erro interno ao buscar chamados.", detalhe = ex.Message });
            }
        }
        // --- FIM DO MÉTODO QUE FALTAVA ---


        // DTO para Status
        public class StatusUpdateRequest
        {
            public string Status { get; set; } = string.Empty;
        }

        // Rota: DELETE para api/chamados/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletarChamado(int id)
        {
            try
            {
                var (usuarioId, errorResult) = GetAndValidateUserId();
                if (errorResult != null)
                {
                    return errorResult;
                }

                var chamado = await _context.Chamado
                    .FirstOrDefaultAsync(c => c.ChamadoID == id && c.UsuarioID == usuarioId.Value);

                if (chamado == null)
                {
                    return NotFound(new { erro = "Chamado não encontrado ou não pertence a este usuário." });
                }

                _context.Chamado.Remove(chamado);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!!!!!!!!! ERRO AO DELETAR CHAMADO !!!!!!!!!\n{ex}");
                return StatusCode(500, new { erro = "Erro interno ao deletar o chamado.", detalhe = ex.Message });
            }
        }

        // Rota: PATCH para api/chamados/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateChamadoStatus(int id, [FromBody] StatusUpdateRequest request)
        {
            try
            {
                var (usuarioId, errorResult) = GetAndValidateUserId();
                if (errorResult != null)
                {
                    return errorResult;
                }

                var statusValidos = new List<string> { "Aberto", "Fechado", "Resolvido", "Em Andamento" };
                if (string.IsNullOrEmpty(request.Status) || !statusValidos.Contains(request.Status))
                {
                    return BadRequest(new { erro = $"Status inválido. Use um dos: {string.Join(", ", statusValidos)}" });
                }

                var chamado = await _context.Chamado
                    .FirstOrDefaultAsync(c => c.ChamadoID == id && c.UsuarioID == usuarioId.Value);

                if (chamado == null)
                {
                    return NotFound(new { erro = "Chamado não encontrado ou não pertence a este usuário." });
                }

                chamado.Status = request.Status;
                chamado.UltimaAtualizacao = DateTime.Now;

                await _context.SaveChangesAsync();
                return Ok(chamado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!!!!!!!!! ERRO AO ATUALIZAR STATUS !!!!!!!!!\n{ex}");
                return StatusCode(500, new { erro = "Erro interno ao atualizar status do chamado.", detalhe = ex.Message });
            }
        }
    }
}