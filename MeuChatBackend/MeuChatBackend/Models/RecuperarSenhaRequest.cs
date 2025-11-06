// Models/RecuperarSenhaRequest.cs
public class RecuperarSenhaRequest
{
    public string RA { get; set; }
    public string Email { get; set; }
    public string Telefone { get; set; }
    public string NovaSenha { get; set; }
    public string ConfirmarNovaSenha { get; set; }
}