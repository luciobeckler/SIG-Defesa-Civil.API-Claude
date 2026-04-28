using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SIG_Defesa_Civil.API.Data.Models;
using SIG_Defesa_Civil.API.Data.Models.SharePoint.Configuration;
using SIG_Defesa_Civil.API.Enums;
using SIG_Defesa_Civil.API.Services.Ocorrencia;
using SIG_Defesa_Civil.API.Services.SharePoint;
using SIG_Defesa_Civil.API.Services.SharePoint.SIG_Defesa_Civil.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SIG Defesa Civil API",
        Version = "v1",
        Description = "API REST para gestão de ocorrências da Defesa Civil de Sabará",
        Contact = new OpenApiContact
        {
            Name = "Prefeitura de Sabará - Defesa Civil",
            Email = "defesacivil@sabara.mg.gov.br"
        }
    });

    // Incluir comentários XML na documentação
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.Configure<SharePointSettings>(
    builder.Configuration.GetSection("SharePointSettings"));
builder.Services.AddScoped<ISharePointService, SharePointService>();
builder.Services.AddScoped<IOcorrenciaService, OcorrenciaService>();

ConnectionStrings connectionStrings = builder.Environment.IsDevelopment()
    ? ConnectionStrings.DEVCONNECTION
    : ConnectionStrings.PRODCONNECTION;

builder.Services.AddDbContext<DefesaCivilContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString(connectionStrings.ToString())));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SIG Defesa Civil v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();