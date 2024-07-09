using CartaoCard.Components;
using CartaoCard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Adicionar Serilog ao Host
builder.Host.UseSerilog();

// Adicionar serviços ao contêiner
builder.Services.AddHttpClient<CieloService>();
builder.Services.AddSingleton<CieloService>();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configurar o middleware de antiforgery
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



var app = builder.Build();


// Configurar o pipeline de solicitação HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Importante: UseRouting deve vir antes de UseEndpoints
app.UseRouting();

// Adicionar middleware de antiforgery aqui
app.UseAntiforgery();

app.MapBlazorHub();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

try
{
    Log.Information("Iniciando a aplicação");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "A aplicação falhou ao iniciar");
}
finally
{
    Log.CloseAndFlush();
}
