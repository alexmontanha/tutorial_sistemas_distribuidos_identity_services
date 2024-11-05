using Microsoft.EntityFrameworkCore;
using MeuProjetoAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MeuProjetoAPI.Data;

public class AppDbContext: IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Produto> Produtos { get; set; }
}