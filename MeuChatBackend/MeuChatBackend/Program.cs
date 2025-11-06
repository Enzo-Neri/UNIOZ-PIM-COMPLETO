using Microsoft.EntityFrameworkCore;
using MeuChatBackend.Data;
using System.Text; // <-- Adicionado para o JWT
using Microsoft.IdentityModel.Tokens; // <-- Adicionado para o JWT

var builder = WebApplication.CreateBuilder(args);

// --- 1. DEFINIR A POLÍTICA DE CORS ---
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
                      policy =>
                      {
                          // Endereço padrão do Live Server
                          policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500") 
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// --- 2. ADICIONAR SERVIÇOS ---
builder.Services.AddControllers();
builder.Services.AddAuthorization(); 

// Adiciona o DbContext (Você já tinha isso, está correto)
builder.Services.AddDbContext<HelpDeskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 3. ADICIONAR O SERVIÇO DE AUTENTICAÇÃO JWT (FALTAVA ISSO) ---
// Isso "ensina" a API a ler e validar os tokens
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            ),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });


// --- Configuração do Swagger/OpenAPI (Opcional, mas bom ter) ---
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure o pipeline de requisições HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// --- 4. ADICIONAR OS MIDDLEWARES NA ORDEM CORRETA ---

// Habilita o CORS (tem que vir antes de Authentication/Authorization)
app.UseCors(myAllowSpecificOrigins);

// Habilita a Autenticação (FALTAVA ISSO)
// (Verifica quem é o usuário em cada requisição)
app.UseAuthentication();

// Habilita a Autorização (Você já tinha)
// (Verifica se o usuário tem permissão)
app.UseAuthorization();

app.MapControllers();

app.Run();