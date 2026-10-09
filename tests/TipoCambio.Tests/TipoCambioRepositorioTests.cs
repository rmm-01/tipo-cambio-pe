using Microsoft.EntityFrameworkCore.Diagnostics;
using TipoCambio.Core;
using TipoCambio.Datos;
using TipoCambio.Tests.Fakes;

namespace TipoCambio.Tests;

public sealed class TipoCambioRepositorioTests : IDisposable
{
    private readonly BaseEnMemoria _base = new();
    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Guardar_YObtener_DevuelveElMismoValor()
    {
        var valor = Dia(9, 3.446m, "SUNAT");
        await Repositorio().GuardarSiNoExisteAsync(valor);

        // Otro contexto: obliga a leer de la base y no del change tracker.
        var leido = await Repositorio().ObtenerAsync(new DateOnly(2026, 10, 9));

        Assert.Equal(valor, leido);
    }

    [Fact]
    public async Task Obtener_FechaSinDato_DevuelveNull()
    {
        Assert.Null(await Repositorio().ObtenerAsync(new DateOnly(2026, 10, 9)));
    }

    [Fact]
    public async Task Guardar_FechaRepetida_ConservaElPrimerValor()
    {
        await Repositorio().GuardarSiNoExisteAsync(Dia(9, 3.446m, "SUNAT"));
        await Repositorio().GuardarSiNoExisteAsync(Dia(9, 9.999m, "BCRP"));

        var leido = await Repositorio().ObtenerAsync(new DateOnly(2026, 10, 9));

        Assert.Equal(3.446m, leido!.Compra);
        Assert.Equal("SUNAT", leido.Fuente);
    }

    [Fact]
    public async Task Guardar_OtraPeticionGuardaLaMismaFechaJustoAntes_NoFalla()
    {
        // Reproduce la carrera: el interceptor inserta la fecha desde otro contexto
        // después del AnyAsync y antes del SaveChanges, así el índice único rechaza el segundo insert.
        var interceptor = new GuardarAntesInterceptor(() =>
            Repositorio().GuardarSiNoExisteAsync(Dia(9, 3.446m, "SUNAT")));
        var repositorio = new TipoCambioRepositorio(_base.CrearContexto(interceptor), _reloj);

        await repositorio.GuardarSiNoExisteAsync(Dia(9, 9.999m, "BCRP"));

        Assert.True(interceptor.SeEjecuto);
        var guardados = await Repositorio().ListarAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        Assert.Equal("SUNAT", Assert.Single(guardados).Fuente);
    }

    [Fact]
    public async Task Listar_IncluyeExtremos_YOrdenaPorFecha()
    {
        foreach (var dia in new[] { 10, 5, 7, 9, 6 })
            await Repositorio().GuardarSiNoExisteAsync(Dia(dia, 3.4m, "SUNAT"));

        var lista = await Repositorio().ListarAsync(new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 9));

        Assert.Equal([6, 7, 9], lista.Select(d => d.Fecha.Day));
    }

    public void Dispose() => _base.Dispose();

    private TipoCambioRepositorio Repositorio() => new(_base.CrearContexto(), _reloj);

    private static TipoCambioDia Dia(int dia, decimal compra, string fuente) =>
        new(new DateOnly(2026, 10, dia), compra, compra + 0.007m, fuente);

    private sealed class GuardarAntesInterceptor(Func<Task> accion) : SaveChangesInterceptor
    {
        public bool SeEjecuto { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!SeEjecuto)
            {
                SeEjecuto = true;
                await accion();
            }

            return result;
        }
    }
}
