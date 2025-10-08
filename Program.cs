using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using WebApiAlmacen.Filters;
using WebApiAlmacen.Middlewares;
using WebApiAlmacen.Models;
using WebApiAlmacen.Repository;
using WebApiAlmacen.Services;
using WebApiAlmacen.Services.GestorArchivos;
using WebApiAlmacen.Services.Hash;
using WebApiAlmacen.Services.TestServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Configuraci�n agregada a AddControllers para prevenir ciclos en las consultas que usen include (eager loading)
// Tambi�n agregamos el filtro de excepci�n global
builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(ExceptionFilter));
    options.Filters.Add(typeof(ModifyResponseFilter));
}).AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Capturamos del app.settings la cadena de conexi�n a la base de datos
// Configuration.GetConnectionString va directamente a la propiedad ConnectionStrings y de ah� tomamos el valor de DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Nuestros servicios resolver�n dependencias de otras clases
// Registramos en el sistema de inyecci�n de dependencias de la aplicaci�n el ApplicationDbContext
builder.Services.AddDbContext<MiAlmacenContext>(options =>
{
    options.UseSqlServer(connectionString);
    // options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();

builder.Services.AddTransient<TransientService>();
builder.Services.AddScoped<ScopedService>();
builder.Services.AddSingleton<SingletonService>();
builder.Services.AddScoped<TestService>();
builder.Services.AddSingleton<ContadorConsultaFamilias>();

builder.Services.AddScoped<IFamiliaRepository, FamiliaRepository>();
builder.Services.AddTransient<OperacionesService>();
builder.Services.AddTransient<IGestorArchivos, GestorArchivosLocal>();
builder.Services.AddTransient<IHashService, HashService>();


// builder.Services.AddHostedService<TareaProgramadaService>();

// Especificamos que cuando apliquemos cach�, esta durar� 30 segundos
builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(30);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        //builder.WithOrigins("http://127.0.0.1:5500").AllowAnyMethod().AllowAnyHeader();
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateIssuer = false,
                   ValidateAudience = false,
                   ValidateLifetime = true,
                   ValidateIssuerSigningKey = true,
                   IssuerSigningKey = new SymmetricSecurityKey(
                     Encoding.UTF8.GetBytes(builder.Configuration["ClaveJWT"]))
               });


builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                        new string[]{}
                    }
                });

});

// Serilog
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<LogsMiddleware>();

app.UseAuthorization();
app.UseOutputCache();

app.MapControllers();

app.Run();
