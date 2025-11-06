// newPass.js
document.addEventListener('DOMContentLoaded', () => {
    const newPassForm = document.getElementById('newPassForm');
    const senhaInput = document.getElementById('senhaInput');
    const confirmarSenhaInput = document.getElementById('confirmarSenhaInput');
    const mensagemDiv = document.getElementById('mensagem');

    // Pegamos o "Passe Livre" (JWT) que salvamos na página anterior
    const resetToken = sessionStorage.getItem('resetToken');

    // Se não tem token, nem adianta, manda pro login
    if (!resetToken) {
        mensagemDiv.textContent = 'Token de redefinição não encontrado. Redirecionando...';
        mensagemDiv.style.color = 'red';
        setTimeout(() => { window.location.href = 'index.html'; }, 3000);
        return;
    }

    newPassForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        mensagemDiv.textContent = '';

        const novaSenha = senhaInput.value;
        const confirmarSenha = confirmarSenhaInput.value;

        // 1. Validação no front-end
        if (novaSenha !== confirmarSenha) {
            mensagemDiv.textContent = 'As senhas não conferem.';
            mensagemDiv.style.color = 'red';
            return;
        }

        if (novaSenha.length < 6) { // Adicione sua regra de senha aqui
            mensagemDiv.textContent = 'A senha deve ter pelo menos 6 caracteres.';
            mensagemDiv.style.color = 'red';
            return;
        }

        // 2. Chamar a API (a rota [Authorize])
        try {
            const response = await fetch('http://localhost:5234/api/recuperarsenha/redefinir', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    // AQUI está a mágica: enviamos o "Passe Livre"
                    'Authorization': `Bearer ${resetToken}`
                },
                body: JSON.stringify({
                    NovaSenha: novaSenha,
                    ConfirmarSenha: confirmarSenha
                })
            });

            if (response.ok) {
                // Deu certo!
                sessionStorage.removeItem('resetToken'); // Limpa o token usado
                mensagemDiv.textContent = 'Senha redefinida com sucesso! Redirecionando...';
                mensagemDiv.style.color = 'green';
                setTimeout(() => { window.location.href = 'index.html'; }, 3000);
            } else {
                // Erro (Ex: "As senhas não conferem" do backend, ou token expirou)
                const textoErro = await response.text();
                mensagemDiv.textContent = textoErro;
                mensagemDiv.style.color = 'red';
            }

        } catch (error) {
            mensagemDiv.textContent = 'Não foi possível conectar ao servidor.';
            mensagemDiv.style.color = 'red';
            console.error('Erro de rede:', error);
        }
    });
});