using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
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

// La documentación interactiva solo se expone en desarrollo, no en producción.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opciones => opciones.WithTitle("API de tipo de cambio (SUNAT/BCRP)"));
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.MapGet("/tipo-cambio/hoy", async Task<Results<Ok<TipoCambioDia>, ProblemHttpResult>> (
    TipoCambioServicio servicio, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        return TypedResults.Ok(await servicio.ObtenerHoyAsync(ct));
    }
    catch (ProveedorNoDisponibleException ex)
    {
        logger.LogError(ex, "No se pudo obtener el tipo de cambio de ninguna fuente.");
        // 503 y no 500: el fallo es de la fuente externa, no de esta API.
        return TypedResults.Problem(
            title: "Fuente de tipo de cambio no disponible",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("ObtenerTipoCambioHoy")
.WithSummary("Tipo de cambio del día")
.WithDescription(
    "Devuelve el tipo de cambio oficial (soles por dólar). Si ya está guardado, no consulta fuentes externas. " +
    "Fuente principal: SUNAT. Si SUNAT no responde, usa BCRP, que publica con retraso: " +
    "en ese caso 'fecha' es el último día publicado y 'fuente' es \"BCRP\".")
.ProducesProblem(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/tipo-cambio/historico", async Task<Results<Ok<IReadOnlyList<TipoCambioDia>>, ValidationProblem>> (
    [Description("Fecha inicial, incluida (yyyy-MM-dd).")] DateOnly desde,
    [Description("Fecha final, incluida (yyyy-MM-dd).")] DateOnly hasta,
    TipoCambioServicio servicio,
    CancellationToken ct) =>
{
    var error = TipoCambioServicio.ValidarRango(desde, hasta);
    if (error is not null)
        return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["rango"] = [error] });

    return TypedResults.Ok(await servicio.ObtenerHistoricoAsync(desde, hasta, ct));
})
.WithName("ObtenerTipoCambioHistorico")
.WithSummary("Histórico por rango de fechas")
.WithDescription(
    $"Devuelve los días guardados entre 'desde' y 'hasta', ordenados por fecha. " +
    $"Solo incluye días que la API ya consultó. Rango máximo: {TipoCambioServicio.MaxDiasHistorico} días.");

app.Run();
