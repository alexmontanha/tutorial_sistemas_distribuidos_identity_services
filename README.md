# Passo a Passo: Configurando o Identity Service com APIs Mínimas e Entity Framework

## 1. **Instalar o Identity Service no projeto**

Primeiro, adicione o pacote `Microsoft.AspNetCore.Identity.EntityFrameworkCore` ao seu projeto, que permitirá integrar o Identity com o Entity Framework Core.

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

## 2. **Configurar o Identity no DbContext**

Em seu arquivo `AppDbContext` (ou equivalente), configure o Identity, integrando-o ao seu contexto de dados. Modifique o `AppDbContext` para herdar de `IdentityDbContext`, o que adiciona as tabelas e estruturas do Identity ao seu banco de dados.

```csharp
// Data/AppDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MeuProjetoAPI.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Sua DbSet para outras entidades, se houver
        // public DbSet<Produto> Produtos { get; set; }
    }
}
```

## 3. **Configurar o Identity no Serviço de Aplicação**

Em `Program.cs`, adicione o Identity aos serviços do aplicativo. Configure o Identity e seu contexto para usar a mesma string de conexão já configurada para o Entity Framework.

```csharp
// Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MeuProjetoAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Configurar o DbContext com a string de conexão
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Adicionar o Identity com o EF Core
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Configurar autenticação com Identity
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware para autenticação
app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

## 4. **Criar Endpoints para Autenticação e Registro**

Com o Identity configurado, você pode criar endpoints para registrar e autenticar usuários.

### Endpoint de Registro

Este endpoint criará um novo usuário e retornará uma resposta se o registro for bem-sucedido ou se houver erros.

```csharp
// Program.cs ou arquivo específico de configuração dos endpoints

app.MapPost("/register", async (UserManager<IdentityUser> userManager, string username, string password) =>
{
    var user = new IdentityUser { UserName = username, Email = username };
    var result = await userManager.CreateAsync(user, password);

    if (result.Succeeded)
    {
        return Results.Ok(new { Message = "User created successfully" });
    }
    return Results.BadRequest(result.Errors);
});
```

### Endpoint de Login

Este endpoint autentica o usuário e gera um token JWT, que pode ser usado para acessar recursos protegidos.

Para isso, você precisará instalar o pacote `Microsoft.AspNetCore.Authentication.JwtBearer`.

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

Em seguida, configure o JWT no `Program.cs`:

```csharp
// Program.cs

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var jwtKey = "sua-chave-secreta-aqui"; // Use uma chave secreta mais segura e longa
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});
```

Agora, adicione o endpoint de login:

```csharp
// Program.cs ou arquivo específico de configuração dos endpoints

app.MapPost("/login", async (UserManager<IdentityUser> userManager, string username, string password) =>
{
    var user = await userManager.FindByNameAsync(username);
    if (user != null && await userManager.CheckPasswordAsync(user, password))
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Results.Ok(new { Token = tokenString });
    }
    return Results.Unauthorized();
});
```

## 5. **Protegendo Endpoints com [Authorize]**

Para garantir que apenas usuários autenticados possam acessar certos endpoints, você pode adicionar o atributo `[Authorize]` diretamente aos endpoints.

```csharp
app.MapGet("/protected", [Authorize] () => "This is a protected endpoint")
    .RequireAuthorization();
```

## 6. **Testar o Sistema de Autenticação**

- Registre um novo usuário usando o endpoint `/register` passando um `username` e `password`.
- Faça login com o usuário registrado usando o endpoint `/login` para obter o token JWT.
- Use o token recebido para acessar o endpoint protegido `/protected`.

### Resumo

Neste tutorial, configuramos o **Identity Service** para autenticação e registro de usuários em uma API mínima no C# com o Entity Framework.
