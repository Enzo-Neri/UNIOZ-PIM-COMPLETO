// home.js - VERSÃO COMPLETA E CORRIGIDA

// --- Seletores e Constantes ---
const abrirChatBtn = document.getElementById('abrirChat');
const fecharChatBtn = document.getElementById('fecharChat');
const janelaChat = document.getElementById('janelaChat');
const areaMensagens = document.getElementById('areaDasMensagens');
const campoInput = document.getElementById('entradaDoChat');
const corpoTabela = document.getElementById('corpo-tabela-chamados');

const API_BASE_URL = 'http://localhost:5234/api/chamados';

// --- Estado da Conversa do Chat ---
let estadoConversa = 'INICIAL';
let chamadoAtualID = null;

const faq = {
    '1': "Para redefinir sua senha, acesse a página de login e clique em 'Esqueci minha senha', preencha todas as colunas e digite seu Token (O mesmo que a faculdade forneceu em sua inscrição).",
    '2': "Nossos boletos são enviados para o seu email cadastrado, em até 5 dias antes do vencimento.",
    '3': "Para ter acesso as suas notas e faltas bimestrais, entre na aba 'Meu Perfil' para ter mais informações.",
    '4': "Para falar com a secretária, ligue para (XX) XXXX-XXXX ou aguarde aqui.",
    '5': "Para cancelar sua matricula ou trancar seu curso, é necessário apresentar presencialmente todos os documentos que foram levados no dia de inscrição."
};

// --- Funções Auxiliares ---

function adicionarNovaMensagem(texto, type) {
    const novaMensagem = document.createElement('p');
    novaMensagem.innerHTML = texto;
    novaMensagem.className = type === 'sistema' ? 'mensagem-sistema' : 'mensagem-usuario';
    areaMensagens.appendChild(novaMensagem);
    areaMensagens.scrollTop = areaMensagens.scrollHeight;
}

function adicionarChamadoNaTabela(chamado) {
    const novaLinha = document.createElement('tr');
    novaLinha.id = `chamado-${chamado.chamadoID}`;

    const dataCriacaoFormatada = new Date(chamado.dataCriacao).toLocaleString('pt-BR');
    const dataAtualizacaoFormatada = new Date(chamado.ultimaAtualizacao).toLocaleString('pt-BR');

    // --- CORREÇÃO DE EXIBIÇÃO 1 ---
    // Agora ele mostra o status real que vem do banco e escolhe a cor certa.
    let statusHTML;
    if (chamado.status === 'Resolvido' || chamado.status === 'Fechado') {
        statusHTML = `<a id="TdGreen" href="#"> <img src="img/check.svg" alt="${chamado.status}" width="10px"> ${chamado.status}</a>`;
    } else {
        // 'Aberto' ou 'Em Andamento'
        statusHTML = `<a id="TdYellow" href="#"> <img src="img/hourglass.svg" alt="${chamado.status}" width="10px"> ${chamado.status}</a>`;
    }

    novaLinha.innerHTML = `
        <td>#${chamado.chamadoID}</td>
        <td>${chamado.assunto}</td>
        <td class="status-cell">${statusHTML}</td>
        <td>${dataCriacaoFormatada}</td>
        <td>${dataAtualizacaoFormatada}</td>
    `;
    corpoTabela.prepend(novaLinha);
}

async function atualizarStatusDoChamado(id, novoStatus) {
    const token = localStorage.getItem('userToken');
    if (!token) return;

    try {
        const response = await fetch(`${API_BASE_URL}/${id}/status`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ status: novoStatus })
        });

        if (!response.ok) {
            // Tenta ler a mensagem de erro da API que fizemos no C#
            const errorText = await response.text(); 
            console.error('API Error:', errorText);
            throw new Error(errorText || 'Falha ao atualizar o status na API');
        }

        const chamadoAtualizado = await response.json();

        // Atualiza a tabela no front-end com os dados que vieram do back-end
        const linhaDoChamado = document.getElementById(`chamado-${id}`);
        if (!linhaDoChamado) return;

        const statusCell = linhaDoChamado.querySelector('.status-cell');
        const atualizacaoCell = linhaDoChamado.cells[4];

        // --- CORREÇÃO DE EXIBIÇÃO 2 ---
        // Mostra o status real que veio da API (ex: "Resolvido")
        statusCell.innerHTML = `<a id="TdGreen" href="#"> <img src="img/check.svg" alt="${chamadoAtualizado.status}" width="10px"> ${chamadoAtualizado.status}</a>`;
        atualizacaoCell.textContent = new Date(chamadoAtualizado.ultimaAtualizacao).toLocaleString('pt-BR');

    } catch (error) {
        console.error("Erro ao atualizar status:", error);
        // Mostra o erro que veio da API (ex: "Status inválido...")
        adicionarNovaMensagem(`Não foi possível atualizar o status do chamado. (Erro: ${error.message})`, 'sistema');
    }
}


// --- Lógica Principal ---

async function carregarChamadosDoUsuario() {
    const token = localStorage.getItem('userToken');
    if (!token) {
        console.error("Nenhum token encontrado. Faça o login.");
        // window.location.href = '/login.html'; 
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/meus-chamados`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (!response.ok) {
            throw new Error('Não foi possível buscar os chamados.');
        }

        const chamados = await response.json();
        corpoTabela.innerHTML = ''; // Limpa a tabela antes de preencher
        chamados.forEach(chamado => adicionarChamadoNaTabela(chamado));

    } catch (error) {
        console.error("Erro ao carregar chamados:", error);
    }
}


// Evento que roda quando a página é carregada
document.addEventListener('DOMContentLoaded', () => {
    carregarChamadosDoUsuario();
});

// Abrir o Chat
abrirChatBtn.addEventListener('click', () => {
    janelaChat.style.display = 'block';
    if (areaMensagens.children.length === 0) {
        adicionarNovaMensagem("Olá! No que posso te ajudar?", 'sistema');
    }
});

// Fechar o Chat
fecharChatBtn.addEventListener('click', () => {
    janelaChat.style.display = 'none';
});

// Enviar Mensagem no Chat
campoInput.addEventListener('keydown', async (event) => {
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        const mensagemUsuario = campoInput.value.trim();
        if (!mensagemUsuario) return;

        adicionarNovaMensagem(mensagemUsuario, 'usuario');
        campoInput.value = '';

        setTimeout(async () => {
            switch (estadoConversa) {
                case 'INICIAL':
                    adicionarNovaMensagem("Enquanto aguarda, por favor, digite o assunto do seu chamado.", 'sistema');
                    estadoConversa = 'AGUARDANDO_ASSUNTO';
                    break;

                case 'AGUARDANDO_ASSUNTO':
                    try {
                        const token = localStorage.getItem('userToken');
                        if (!token) {
                            adicionarNovaMensagem("Erro de autenticação. Por favor, faça login novamente.", 'sistema');
                            return;
                        }

                        const response = await fetch(API_BASE_URL, {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json',
                                'Authorization': `Bearer ${token}`
                            },
                            body: JSON.stringify({ assunto: mensagemUsuario })
                        });

                        if (!response.ok) {
                            const errorData = await response.json();
                            throw new Error(errorData.message || 'Falha na API');
                        }

                        const chamadoCriado = await response.json();
                        chamadoAtualID = chamadoCriado.chamadoID;
                        adicionarChamadoNaTabela(chamadoCriado);

                        let faqMessage = `Chamado #${chamadoAtualID} criado com sucesso!<br><br>Talvez eu possa te ajudar com alguma informação. Escolha uma opção (digite o número):<br>
                        1. Como redefinir minha senha?<br>
                        2. Onde encontro meu boleto?<br>
                        3. Onde vejo minhas notas e faltas?<br>
                        4. Como falar com o suporte técnico?<br>
                        5. Como trancar o meu curso?`;
                        adicionarNovaMensagem(faqMessage, 'sistema');
                        estadoConversa = 'AGUARDANDO_OPCAO_FAQ';

                    } catch (error) {
                        console.error("Erro ao criar chamado:", error);
                        adicionarNovaMensagem("Desculpe, não consegui registrar seu chamado. Tente novamente.", 'sistema');
                    }
                    break;

                case 'AGUARDANDO_OPCAO_FAQ':
                    if (faq[mensagemUsuario]) {
                        adicionarNovaMensagem(faq[mensagemUsuario], 'sistema');
                        adicionarNovaMensagem("Isso resolveu sua dúvida? (Responda 'sim' ou 'não')", 'sistema');
                        estadoConversa = 'AGUARDANDO_CONFIRMACAO_FINAL';
                    } else {
                        adicionarNovaMensagem("Opção inválida. Por favor, digite um número de 1 a 5.", 'sistema');
                    }
                    break;

                case 'AGUARDANDO_CONFIRMACAO_FINAL':
                    const respostaFinal = mensagemUsuario.toLowerCase();
                    if (respostaFinal.includes('sim')) {
                        adicionarNovaMensagem("Que ótimo! Seu chamado será marcado como resolvido.", 'sistema');
                        
                        // --- A CORREÇÃO PRINCIPAL ---
                        // Trocamos 'Finalizado' por 'Resolvido', que está na "lista VIP" do banco.
                        atualizarStatusDoChamado(chamadoAtualID, 'Resolvido');

                    } else {
                        adicionarNovaMensagem("Ok. Um atendente dará continuidade ao seu chamado em breve.", 'sistema');
                        // Não fazemos nada, o status continua "Aberto"
                    }
                    campoInput.disabled = true;
                    campoInput.placeholder = "Atendimento encerrado.";
                    estadoConversa = 'FINALIZADO';
                    break;
            }
        }, 800);
    }
});

/**
 * Decodifica um token JWT (que está no localStorage)
 * para ler as informações (claims) de dentro dele.
 */
function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    } catch (e) {
        console.error("Erro ao decodificar o token:", e);
        return null;
    }
}