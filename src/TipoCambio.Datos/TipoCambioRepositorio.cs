using Microsoft.EntityFrameworkCore;
using TipoCambio.Core;

namespace TipoCambio.Datos;

public sealed class TipoCambioRepositorio(TipoCambioDbContext db, TimeProvider reloj) : ITipoCambioRepositorio
{
    public async Task<TipoCambioDia?> ObtenerAsync(DateOnly fecha, CancellationToken cancellationToken = default)
    {
        var registro = await db.TiposCambio.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Fecha == fecha, cancellationToken);

        return registro is null ? null : AModelo(registro);
    }

    public async Task GuardarSiNoExisteAsync(TipoCambioDia tipoCambio, CancellationToken cancellationToken = default)
    {
        if (await db.TiposCambio.AnyAsync(x => x.Fecha == tipoCambio.Fecha, cancellationToken))
            return;

        var registro = new TipoCambioRegistro
        {
            Fecha = tipoCambio.Fecha,
            Compra = tipoCambio.Compra,
            Venta = tipoCambio.Venta,
            Fuente = tipoCambio.Fuente,
            RegistradoEn = reloj.GetUtcNow(),
        };
        db.TiposCambio.Add(registro);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Otra petición guardó la misma fecha entre el AnyAsync y el SaveChanges: el índice único la frenó.
            // Si la fecha no está en la base, el error es otro y debe verse.
            db.Entry(registro).State = EntityState.Detached;
            if (!await ExisteEnBaseAsync(tipoCambio.Fecha, cancellationToken))
                throw;
        }
    }

    public async Task<IReadOnlyList<TipoCambioDia>> ListarAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        var registros = await db.TiposCambio.AsNoTracking()
            .Where(x => x.Fecha >= desde && x.Fecha <= hasta)
            .OrderBy(x => x.Fecha)
            .ToListAsync(cancellationToken);

        return registros.Select(AModelo).ToList();
    }

    private Task<bool> ExisteEnBaseAsync(DateOnly fecha, CancellationToken cancellationToken) =>
        db.TiposCambio.AsNoTracking().AnyAsync(x => x.Fecha == fecha, cancellationToken);

    private static TipoCambioDia AModelo(TipoCambioRegistro r) => new(r.Fecha, r.Compra, r.Venta, r.Fuente);
}
