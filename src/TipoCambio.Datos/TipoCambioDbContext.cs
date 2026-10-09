using Microsoft.EntityFrameworkCore;

namespace TipoCambio.Datos;

public sealed class TipoCambioDbContext(DbContextOptions<TipoCambioDbContext> options) : DbContext(options)
{
    public DbSet<TipoCambioRegistro> TiposCambio => Set<TipoCambioRegistro>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TipoCambioRegistro>(e =>
        {
            e.ToTable("TiposCambio");

            // Un solo valor por día: la base lo garantiza aunque lleguen dos peticiones a la vez.
            e.HasIndex(x => x.Fecha).IsUnique();

            e.Property(x => x.Compra).HasPrecision(10, 4);
            e.Property(x => x.Venta).HasPrecision(10, 4);
            e.Property(x => x.Fuente).HasMaxLength(20);
        });
    }
}
