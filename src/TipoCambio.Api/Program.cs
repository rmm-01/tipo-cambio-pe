using Microsoft.EntityFrameworkCore;
using TipoCambio.Core;
using TipoCambio.Core.Bcrp;
using TipoCambio.Core.Sunat;
using TipoCambio.Datos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

// Sin User-Agent, BCRP responde 200 con una página HTML anti-bots en lugar del JSON.
const string UserAgent = "TipoCambioPe/1.0 (+https://github.com/rmm-01/tipo-cambio-pe)";

builder.Services.AddHttpClient<SunatProveedor>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["Sunat:BaseUrl"] ?? "https://www.sunat.gob.pe/");
    http.Timeout = TimeSpan.FromSeconds(10);
    http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
});
builder.Services.AddHttpClient<BcrpProveedor>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["Bcrp:BaseUrl"] ?? "https://estadisticas.bcrp.gob.pe/");
    http.Timeout = TimeSpan.FromSeconds(10);
    http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
});

// SUNAT es la fuente principal; BCRP entra solo si SUNAT falla.
builder.Services.AddTransient<ITipoCambioProveedor>(sp => new ProveedorConRespaldo(
    sp.GetRequiredService<SunatProveedor>(),
    sp.GetRequiredService<BcrpProveedor>(),
    sp.GetRequiredService<ILogger<ProveedorConRespaldo>>()));

builder.Services.AddDbContext<TipoCambioDbContext>(db =>
    db.UseSqlite(builder.Configuration.GetConnectionString("TipoCambio") ?? "Data Source=tipocambio.db"));
builder.Services.AddScoped<ITipoCambioRepositorio, TipoCambioRepositorio>();
builder.Services.AddScoped<TipoCambioServicio>();

var app = builder.Build();

// Crea el archivo SQLite y aplica las migraciones pendientes al iniciar: quien clone el repo no instala nada.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<TipoCambioDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/tipo-cambio/hoy", async (TipoCambioServicio servicio, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await servicio.ObtenerHoyAsync(ct));
    }
    catch (ProveedorNoDisponibleException ex)
    {
        logger.LogError(ex, "No se pudo obtener el tipo de cambio de ninguna fuente.");
        // 503 y no 500: el fallo es de la fuente externa, no de esta API.
        return Results.Problem(
            title: "Fuente de tipo de cambio no disponible",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("ObtenerTipoCambioHoy");

app.MapGet("/tipo-cambio/historico", async (DateOnly desde, DateOnly hasta, TipoCambioServicio servicio, CancellationToken ct) =>
{
    var error = TipoCambioServicio.ValidarRango(desde, hasta);
    if (error is not null)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["rango"] = [error] });

    return Results.Ok(await servicio.ObtenerHistoricoAsync(desde, hasta, ct));
})
.WithName("ObtenerTipoCambioHistorico");

app.Run();
