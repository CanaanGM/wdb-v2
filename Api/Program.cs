using System.Diagnostics;
using Api.Application.Cqrs;
using Api.Common.Errors;
using Api.Common.Mcp;
using Api.Features.Exercises;
using Api.Features.Muscles;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("McpLocalCors", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            try
            {
                return new Uri(origin).IsLoopback;
            }
            catch
            {
                return false;
            }
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd(
            "traceId",
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
    };
});
builder.Services.AddCqrs();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExerciseFeature();
builder.Services.AddMuscleFeature();
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(WorkoutLogReadTools).Assembly);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/mcp"),
    branch => branch.UseHttpsRedirection());

app.UseCors();
app.UseAuthorization();

app.MapControllers();
var mcpEndpoint = app.MapMcp("/mcp");
mcpEndpoint.RequireCors("McpLocalCors");

app.Run();
