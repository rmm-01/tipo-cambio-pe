using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TipoCambio.Datos;

namespace TipoCambio.Tests.Fakes;

/// <summary>
/// SQLite real pero en memoria: prueba las consultas y el índice único sin crear archivos.
/// La base vive mientras la conexión esté abierta, así que se comparte entre contextos de la misma prueba.
/// </summary>
internal sealed class BaseEnMemoria : IDisposable
{
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    public BaseEnMemoria()
    {
        _conexion.Open();
        using var db = CrearContexto();
        db.Database.Migrate();
    }

    public TipoCambioDbContext CrearContexto(params IInterceptor[] interceptores) =>
        new(new DbContextOptionsBuilder<TipoCambioDbContext>().UseSqlite(_conexion).AddInterceptors(interceptores).Options);

    public void Dispose() => _conexion.Dispose();
}
