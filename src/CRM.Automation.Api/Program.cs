using Microsoft.OpenApi.Models;
using CRM.Automation.Api.Filters;
using CRM.Automation.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with CheckPoint ApiKey Header support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GigaMall CRM Automation API",
        Version = "v1",
        Description = "Modern .NET 8 WebAPI backend for CheckPoint Automation Framework (Tasks: Burn Point, Reset Point, Downgrade Membership)"
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "CheckPoint authentication header. Format: enter your API Key",
        Name = "x-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Register Filters
builder.Services.AddScoped<ApiKeyAuthFilter>();

// Register Infrastructure Services (Dapper, Repositories, Push Notifications, Business Workflows)
builder.Services.AddCrmAutomationInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CRM Automation API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at application root (http://localhost:5000/)
});

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();
