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
    public DbSet<SugestaoEdicao> SugestoesEdicao => Set<SugestaoEdicao>();
    public DbSet<HorarioFuncionamento> HorariosFuncionamento => Set<HorarioFuncionamento>();

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

            e.HasMany(b => b.Horarios)
             .WithOne(h => h.Bicicletario)
             .HasForeignKey(h => h.BicicletarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<HorarioFuncionamento>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.DiaSemana).HasConversion<int>();
            e.HasIndex(h => new { h.BicicletarioId, h.DiaSemana }).IsUnique();
        });

        builder.Entity<SugestaoEdicao>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.DadosEdicao).IsRequired();

            e.HasOne(s => s.Bicicletario)
             .WithMany(b => b.Sugestoes)
             .HasForeignKey(s => s.BicicletarioId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Autor)
             .WithMany(u => u.SugestoesEnviadas)
             .HasForeignKey(s => s.AutorId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Revisor)
             .WithMany()
             .HasForeignKey(s => s.RevisorId)
             .OnDelete(DeleteBehavior.SetNull);
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
