using TipoCambio.Core;
using TipoCambio.Core.Bcrp;
using TipoCambio.Core.Sunat;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/tipo-cambio/hoy", async (ITipoCambioProveedor proveedor, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await proveedor.ObtenerHoyAsync(ct));
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

app.Run();
