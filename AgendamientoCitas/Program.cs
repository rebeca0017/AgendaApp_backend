using System.Text.Json.Serialization;
using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Endpoints;
using AgendamientoCitas.Filtros;
using AgendamientoCitas.Mapping;
using AgendamientoCitas.Repositorios;
using AgendamientoCitas.Servicios;
using AgendamientoCitas.Utilidades;
using AgendamientoCitas.Validaciones;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:3000"];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddAutoMapper(_ => { }, typeof(AgendamientoProfile).Assembly);
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IServicioRepositorio, ServicioRepositorio>();
builder.Services.AddScoped<ICitaRepositorio, CitaRepositorio>();
builder.Services.AddScoped<IIngresoRepositorio, IngresoRepositorio>();
builder.Services.AddScoped<IGastoRepositorio, GastoRepositorio>();
builder.Services.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
builder.Services.AddScoped<IServicioUsuarios, ServicioUsuarios>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<IdentificacionSettings>(builder.Configuration.GetSection("Identificacion"));
builder.Services.AddTransient<IServicioEmail, ServicioEmail>();
builder.Services.AddHttpClient<IValidadorIdentificacion, ValidadorIdentificacion>();
builder.Services.AddScoped<IUserStore<IdentityUser>, UsuarioStore>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddValidatorsFromAssemblyContaining<CredencialesUsuarioDTOValidador>();
builder.Services.AddScoped(typeof(FiltroValidaciones<>));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = Llaves.ObtenerLlave(builder.Configuration).First(),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

app.UseCors("ReactApp");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    name = "AgendamientoCitas API",
    status = "OK",
    dataAccess = "Dapper",
    endpoints = new[] { "/api/clientes", "/api/servicios", "/api/citas", "/api/ingresos", "/api/gastos", "/api/usuarios" }
}));

app.MapGroup("/api/clientes").RequireAuthorization().MapClientes();
app.MapGroup("/api/servicios").RequireAuthorization().MapServicios();
app.MapGroup("/api/citas").RequireAuthorization().MapCitas();
app.MapGroup("/api/ingresos").RequireAuthorization().MapIngresos();
app.MapGroup("/api/gastos").RequireAuthorization().MapGastos();
app.MapGroup("/api/usuarios").MapUsuarios();

app.Run();
