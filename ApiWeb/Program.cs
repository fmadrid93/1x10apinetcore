using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Application.Reportes;



namespace ApiWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================
            // 1) JWT
            // =========================
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            builder.Services.AddAuthorization();

            // =========================
            // 2) Controllers + NewtonsoftJson
            // =========================
            builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                });

            // =========================
            // 3) Swagger
            // =========================
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "ApiWeb",
                    Description = "API",
                    Contact = new OpenApiContact
                    {
                        Name = "Soporte Técnico",
                        Email = "soporte@madridsource.com"
                    }
                });

                // (Opcional) Swagger con JWT (para probar con Bearer)
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "JWT Authorization header usando el esquema Bearer. Ej: \"Bearer {token}\"",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                };
                c.AddSecurityDefinition("Bearer", securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securityScheme, new List<string>() }
                });
            });

            // Clave si usas AddNewtonsoftJson()
            builder.Services.AddSwaggerGenNewtonsoftSupport();

            // =========================
            // 4) SignalR
            // =========================
            builder.Services.AddSignalR();

            // =========================
            // 5) CORS
            // =========================
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    // ✅ Producción: usa WithOrigins("https://tudominio.com")
                    // ✅ Dev Angular: WithOrigins("http://localhost:4200")

                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();

                    // ⚠️ Si usarás SignalR con credenciales/cookies:
                    // policy.WithOrigins("http://localhost:4200")
                    //       .AllowAnyHeader()
                    //       .AllowAnyMethod()
                    //       .AllowCredentials();
                });
            });


            // =========================
            // BUILD
            // =========================
            // =========================
            // 6) Servicios propios
            // =========================
            builder.Services.AddScoped<Application.Alerta.AlertaService>();
            builder.Services.AddHostedService<Application.Jobs.AlertasBackgroundService>();

            builder.Services.AddScoped<Application.Auth.AuthService>();

            builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
            var app = builder.Build();

            // =========================
            // MIDDLEWARE
            // =========================
            app.UseCors("AllowFrontend");

          //  if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiWeb v1");
                    c.RoutePrefix = "swagger";
                });
            }

            app.UseAuthentication();
            app.UseAuthorization();

            // =========================
            // MAPS
            // =========================
            app.MapControllers();

            // =========================
            // RUN
            // =========================
            //   builder.Services.AddSingleton<FfmpegBootstrapper>();
           
            app.Run();
        }
    }
}
