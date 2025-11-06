document.addEventListener('DOMContentLoaded', () => {

    const loginForm = document.getElementById('loginForm');
    const raInput = document.getElementById('raInput');
    const senhaInput = document.getElementById('senhaInput');
    const mensagemErro = document.getElementById('mensagemErro');
    
    // 1. Pegamos o botão de submit para poder desabilitá-lo
    // Tente adicionar um id="loginButton" no seu HTML para ficar mais seguro
    const loginButton = loginForm.querySelector('button[type="submit"]'); 

    loginForm.addEventListener('submit', async (event) => {
        // Previne o recarregamento padrão da página
        event.preventDefault();

        mensagemErro.textContent = ''; // Limpa mensagens de erro antigas

        const ra = raInput.value.trim(); // .trim() limpa espaços em branco
        const senha = senhaInput.value.trim();

        // 2. Validação client-side simples
        if (ra === '' || senha === '') {
            mensagemErro.textContent = 'Por favor, preencha o RA e a Senha.';
            return; // Para a execução da função aqui
        }

        // 3. Feedback visual: desabilita o botão e muda o texto
        loginButton.disabled = true;
        loginButton.textContent = 'Entrando...'; // Opcional, mas bom para UX

        // A URL da sua API de login
        const apiUrl = 'http://localhost:5234/api/login'; 

        try {
            const response = await fetch(apiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    RA: ra,
                    Senha: senha
                }),
            });

            if (response.ok) {
                const dadosResposta = await response.json();
                localStorage.setItem('userToken', dadosResposta.token);
                console.log('Login bem-sucedido, token salvo!');
                window.location.href = 'home.html';
                // Não precisamos reabilitar o botão aqui, pois a página vai mudar
            } else {
                // 4. Melhoria no tratamento de erro
                let erroMsg = 'RA ou Senha inválidos.'; // Uma mensagem padrão
                try {
                    // Tenta ler a resposta de erro como JSON
                    const erroData = await response.json();
                    
                    // Ajuste 'message' ou 'error' para o nome da propriedade que sua API C# retorna
                    if (erroData && erroData.message) { 
                        erroMsg = erroData.message;
                    } else if (erroData && erroData.error) {
                        erroMsg = erroData.error;
                    }
                } catch (e) {
                    // Se não for JSON, apenas pega o texto (como estava antes)
                    const textoErro = await response.text();
                    if (textoErro) {
                        erroMsg = textoErro;
                    }
                }
                mensagemErro.textContent = erroMsg;
                console.error('Falha no login:', erroMsg);
            }

        } catch (error) {
            // Se houver um erro de rede (API caiu, sem internet)
            mensagemErro.textContent = 'Não foi possível conectar ao servidor. Tente novamente mais tarde.';
            console.error('Erro de conexão:', error);
        } finally {
            // 5. O bloco 'finally' SEMPRE roda (dando erro ou não)
            // Se o login falhar, reabilitamos o botão e voltamos o texto.
            // Se o login der certo, a página redireciona antes disso.
            loginButton.disabled = false;
            loginButton.textContent = 'Entrar'; // Coloque o texto original do seu botão
        }
    });
});