using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenCodeSessionServer.Services;

var builder = WebApplication.CreateBuilder(args);

var storagePath = builder.Configuration["Storage:RootPath"] ?? "./sessions";
builder.Services.AddSingleton<ISessionService>(new FileSessionService(storagePath));

var apiKey = builder.Configuration["Authentication:ApiKey"];
if (!string.IsNullOrEmpty(apiKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                RequireSignedTokens = false,
                SignatureValidator = (token, _) => new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token)
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authHeader["Bearer ".Length..].Trim();
                        if (token == apiKey)
                        {
                            context.Token = token;
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization();
}

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
