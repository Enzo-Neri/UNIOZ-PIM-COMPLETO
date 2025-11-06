// recuperar.js
document.addEventListener('DOMContentLoaded', () => {
    const recuperarForm = document.getElementById('recuperarForm');
    const mensagemDiv = document.getElementById('mensagem');

    const raInput = document.getElementById('raInput');
    const emailInput = document.getElementById('emailInput');
    const telefoneInput = document.getElementById('telInput');
    const tokenInput = document.getElementById('tokenInput');

    recuperarForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        mensagemDiv.textContent = '';
        mensagemDiv.style.color = 'red';

        const ra = raInput.value;
        const email = emailInput.value;
        const telefone = telefoneInput.value;
        const token = tokenInput.value;

        // USA O ENDPOINT DE VALIDAR
        const apiUrl = 'http://localhost:5234/api/recuperarsenha/validar';

        try {
            const response = await fetch(apiUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    RA: ra,
                    Email: email,
                    Telefone: telefone,
                    Token: token
                })
            });

            if (response.ok) {
                // SUCESSO! Pegamos o "Passe Livre"
                const data = await response.json();

                // Guardamos no "cofrinho" (sessionStorage)
                sessionStorage.setItem('resetToken', data.token);
                
                // Redireciona para a Página 2
                window.location.href = 'newPass.html';
            } else {
                const textoResposta = await response.text();
                mensagemDiv.textContent = textoResposta; // "Dados inválidos..."
            }

        } catch (error) {
            mensagemDiv.textContent = 'Não foi possível conectar ao servidor.';
            console.error('Erro de rede:', error);
        }
    });
});