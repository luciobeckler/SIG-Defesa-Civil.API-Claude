using SIG_Defesa_Civil.API.Extensions;
using SIG_Defesa_Civil.API.Infrastructure.Seeders;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Serviços ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Força a API a retornar e aceitar Enums como texto (Ex: "ADMIN" em vez de 3)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerConfiguration();
builder.Services.AddDatabaseConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddDependencyInjectionConfiguration(builder.Configuration);

var app = builder.Build();

// ── 2. Views de BI saem de cena ──────────────────────────────────────────────
// Uma view que lê uma coluna impede o PostgreSQL de alterar o tipo dela. Como
// as migrations rodam no passo seguinte, as views precisam sair antes.
await ViewsBiSeeder.RemoverAsync(app.Services);

// ── 3. Migrations + seed do administrador inicial ────────────────────────────
await AdminSeeder.SeedAsync(app.Services);

// ── 4. Views de BI de volta, a partir de Scripts/BI/views_bi.sql ─────────────
await ViewsBiSeeder.SeedAsync(app.Services);

// ── 3. Pipeline de middlewares ────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SIG Defesa Civil v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
