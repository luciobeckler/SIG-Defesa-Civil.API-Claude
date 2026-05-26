namespace SIG_Defesa_Civil.API.Extensions
{
    using Amazon.S3;
    using global::SIG_Defesa_Civil.API.Data.Configuration.Auth;
    using global::SIG_Defesa_Civil.API.Data.Configuration.DocumentTemplate;
    using global::SIG_Defesa_Civil.API.Data.Configuration.Storage;
    using global::SIG_Defesa_Civil.API.Data.Models;
    using global::SIG_Defesa_Civil.API.Data.Models.Tabelas;
    using global::SIG_Defesa_Civil.API.Services.Auth;
    using global::SIG_Defesa_Civil.API.Services.AvaliacaoRisco;
    using global::SIG_Defesa_Civil.API.Services.Documento;
    using global::SIG_Defesa_Civil.API.Services.Encaminhamento;
    using global::SIG_Defesa_Civil.API.Services.Notificacao;
    using global::SIG_Defesa_Civil.API.Services.Ocorrencia;
    using global::SIG_Defesa_Civil.API.Services.Relatorio;
    using global::SIG_Defesa_Civil.API.Services.Storage;
    using global::SIG_Defesa_Civil.API.Services.Vistoria;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using System.Text;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
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
                options.CustomSchemaIds(type => type.FullName);

                // Suporte a JWT no Swagger UI
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Informe o token JWT: Bearer {seu_token}"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath);
            });

            return services;
        }

        public static IServiceCollection AddDatabaseConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment
            )
        {
            string connectionKey = environment.IsDevelopment() ? "DevConnection" : "ProdConnection";

            var connectionString = configuration.GetConnectionString(connectionKey);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"A string de conexão '{connectionKey}' não foi encontrada na configuração.");
            }

            services.AddDbContext<DefesaCivilContext>(options =>
                options.UseNpgsql(connectionString));

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSection);

            var jwtSettings = jwtSection.Get<JwtSettings>()!;
            var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();

            return services;
        }

        public static IServiceCollection AddCorsConfiguration(
            this IServiceCollection services, IConfiguration configuration)
        {
            var origins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                          ?? ["http://localhost:8100", "http://localhost:4200"];

            services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    policy
                        .WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }

        public static IServiceCollection AddDependencyInjectionConfiguration(
            this IServiceCollection services, IConfiguration configuration)
        {
            // Configurações (Options)
            services.Configure<StorageSettings>(configuration.GetSection("StorageSettings"));
            services.Configure<TemplateSettings>(configuration.GetSection("TemplateSettings"));
            services.Configure<AdminSeedSettings>(configuration.GetSection("AdminSeed"));

            // Hash de senha (PBKDF2 via ASP.NET Core Identity — sem EF Identity)
            services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

            // Serviços — Auth
            services.AddScoped<IAuthService, AuthService>();

            // ── Storage: R2 (produção) ou LocalFileSystem (desenvolvimento) ────────
            // Ativação automática: se R2Storage:AccountId estiver preenchido → R2.
            // Variáveis de ambiente no Render: R2Storage__AccountId, R2Storage__AccessKeyId,
            //   R2Storage__SecretAccessKey, R2Storage__BucketName
            var r2Settings = configuration.GetSection("R2Storage").Get<R2StorageSettings>();
            if (r2Settings?.IsConfigured == true)
            {
                services.Configure<R2StorageSettings>(configuration.GetSection("R2Storage"));
                services.AddSingleton<IAmazonS3>(_ =>
                {
                    var s3Config = new AmazonS3Config
                    {
                        ServiceURL = r2Settings.ServiceUrl,
                        ForcePathStyle = true,
                    };
                    return new AmazonS3Client(
                        r2Settings.AccessKeyId,
                        r2Settings.SecretAccessKey,
                        s3Config);
                });
                services.AddScoped<IStorageService, R2StorageService>();
            }
            else
            {
                services.AddScoped<IStorageService, LocalFileSystemStorageService>();
            }
            services.AddScoped<IOcorrenciaService, OcorrenciaService>();
            services.AddScoped<IDocumentoService, DocumentoService>();
            services.AddScoped<IAvaliacaoRiscoService, AvaliacaoRiscoService>();
            services.AddScoped<IVistoriaService, VistoriaService>();
            services.AddScoped<INotificacaoService, NotificacaoService>();
            services.AddScoped<IEncaminhamentoService, EncaminhamentoService>();
            services.AddScoped<IRelatorioService, RelatorioService>();

            return services;
        }
    }
}
