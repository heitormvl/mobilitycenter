using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MobilityCenter.Shared.Models;

namespace MobilityCenter.Repositories.Context;

public class MobilityCenterDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
{
    public MobilityCenterDbContext(DbContextOptions<MobilityCenterDbContext> options) : base(options) { }

    public DbSet<Bicicletario> Bicicletarios => Set<Bicicletario>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Usuario>(e =>
        {
            e.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
        });

        builder.Entity<Bicicletario>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Nome).IsRequired().HasMaxLength(200);
            e.Property(b => b.Location).HasColumnType("geometry(Point, 4326)");
            e.Property(b => b.VeiculosSuportados).HasConversion<int>();
            e.HasQueryFilter(b => !b.Deletado);
            e.HasIndex(b => b.Location).HasMethod("gist");

            e.HasOne(b => b.Operador)
             .WithMany(u => u.Bicicletarios)
             .HasForeignKey(b => b.OperadorId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(b => b.Avaliacoes)
             .WithOne(a => a.Bicicletario)
             .HasForeignKey(a => a.BicicletarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Avaliacao>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Nota).HasColumnType("smallint");
            e.ToTable(t => t.HasCheckConstraint("CK_Avaliacao_Nota", "\"Nota\" BETWEEN 1 AND 5"));
            e.HasQueryFilter(a => a.Bicicletario == null || !a.Bicicletario.Deletado);

            e.HasOne(a => a.Usuario)
             .WithMany(u => u.Reviews)
             .HasForeignKey(a => a.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
