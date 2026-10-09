using Microsoft.Extensions.Logging.Abstractions;
using TipoCambio.Core;

namespace TipoCambio.Tests;

public class ProveedorConRespaldoTests
{
    private static readonly TipoCambioDia DeSunat = new(new DateOnly(2026, 10, 9), 3.446m, 3.453m, "SUNAT");
    private static readonly TipoCambioDia DeBcrp = new(new DateOnly(2026, 10, 6), 3.431m, 3.437m, "BCRP");

    [Fact]
    public async Task PrincipalResponde_NoConsultaRespaldo()
    {
        var principal = new ProveedorFalso(() => DeSunat);
        var respaldo = new ProveedorFalso(() => DeBcrp);

        var resultado = await Crear(principal, respaldo).ObtenerHoyAsync();

        Assert.Equal(DeSunat, resultado);
        Assert.Equal(0, respaldo.Llamadas);
    }

    [Fact]
    public async Task PrincipalFalla_UsaRespaldo()
    {
        var principal = new ProveedorFalso(() => throw new ProveedorNoDisponibleException("SUNAT", "caído"));
        var respaldo = new ProveedorFalso(() => DeBcrp);

        var resultado = await Crear(principal, respaldo).ObtenerHoyAsync();

        Assert.Equal(DeBcrp, resultado);
        Assert.Equal(1, respaldo.Llamadas);
    }

    [Fact]
    public async Task AmbosFallan_LanzaProveedorNoDisponibleConAmbasFuentes()
    {
        var principal = new ProveedorFalso(() => throw new ProveedorNoDisponibleException("SUNAT", "caído"));
        var respaldo = new ProveedorFalso(() => throw new ProveedorNoDisponibleException("BCRP", "caído"));

        var ex = await Assert.ThrowsAsync<ProveedorNoDisponibleException>(() => Crear(principal, respaldo).ObtenerHoyAsync());

        Assert.Equal("SUNAT y BCRP", ex.Fuente);
        Assert.Equal(2, Assert.IsType<AggregateException>(ex.InnerException).InnerExceptions.Count);
    }

    [Fact]
    public async Task ErrorInesperadoDelPrincipal_NoActivaRespaldo()
    {
        // Solo "fuente no disponible" activa el respaldo; un bug propio debe verse, no esconderse.
        var principal = new ProveedorFalso(() => throw new InvalidOperationException("bug"));
        var respaldo = new ProveedorFalso(() => DeBcrp);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Crear(principal, respaldo).ObtenerHoyAsync());
        Assert.Equal(0, respaldo.Llamadas);
    }

    private static ProveedorConRespaldo Crear(ITipoCambioProveedor principal, ITipoCambioProveedor respaldo) =>
        new(principal, respaldo, NullLogger<ProveedorConRespaldo>.Instance);

    private sealed class ProveedorFalso(Func<TipoCambioDia> obtener) : ITipoCambioProveedor
    {
        public int Llamadas { get; private set; }

        public Task<TipoCambioDia> ObtenerHoyAsync(CancellationToken cancellationToken = default)
        {
            Llamadas++;
            return Task.FromResult(obtener());
        }
    }
}
