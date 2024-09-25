using GrpcService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin() // Use specific origins for production
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .WithHeaders("Authorization", "Content-Type"); ;
        });
});

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true; // For development only; use true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = false,
        ValidIssuer = "YourIssuer",
        ValidAudience = "YourAudience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyHere"))
    };
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxConcurrentConnections = 100;
    serverOptions.Limits.Http2.MaxStreamsPerConnection = 50;
});

var app = builder.Build();

// Ensure CORS is configured before authentication and authorization
app.UseRouting();

app.UseCors("AllowAll"); // Apply CORS policy

app.UseGrpcWeb(); // Enable gRPC-Web support


// Optional custom middleware; ensure it does not interfere with CORS
app.Use(async (context, next) =>
{
    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
    await next();
});

app.MapGrpcService<ChatServices>()
   .EnableGrpcWeb(); // Enable gRPC-Web for the service
app.MapGrpcReflectionService();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();