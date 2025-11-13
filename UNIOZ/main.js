document.addEventListener('DOMContentLoaded', () => {

    const loginForm = document.getElementById('loginForm');
    const raInput = document.getElementById('raInput');
    const senhaInput = document.getElementById('senhaInput');
    const mensagemErro = document.getElementById('mensagemErro');

    const loginButton = loginForm.querySelector('button[type="submit"]');

    loginForm.addEventListener('submit', async (event) => {

        event.preventDefault();

        mensagemErro.textContent = ''; // Limpa mensagens de erro antigas

        const ra = raInput.value.trim(); // .trim() limpa espaços em branco
        const senha = senhaInput.value.trim();

        if (ra === '' || senha === '') {
            mensagemErro.textContent = 'Por favor, preencha o RA e a Senha.';
            return;
        }

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

            } else {



                const erroTexto = await response.text();
                let erroMsg = erroTexto;


                try {
                    const erroJson = JSON.parse(erroTexto);
                    // Se for JSON, procura por uma mensagem mais bonita
                    if (erroJson && erroJson.erro) {
                        erroMsg = erroJson.erro;
                    } else if (erroJson && erroJson.message) {
                        erroMsg = erroJson.message;
                    }
                } catch (e) {

                }


                if (!erroMsg) {
                    if (response.status === 401 || response.status === 400) {
                        erroMsg = "RA ou Senha inválidos.";
                    } else {
                        erroMsg = "Ocorreu um erro no servidor. Tente novamente.";
                    }
                }

                mensagemErro.textContent = erroMsg;
                console.error('Falha no login:', response.status, erroMsg);


            }

        } catch (error) {

            mensagemErro.textContent = 'Não foi possível conectar ao servidor. Tente novamente mais tarde.';
            console.error('Erro de conexão:', error);
        } finally {

            loginButton.disabled = false;
            loginButton.textContent = 'Entrar';
        }
    });
});