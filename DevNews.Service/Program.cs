using DevNews.Service.ExceptionHandling;
using DevNews.Service.Infrastructure.Weather;
using DevNews.Services.Application.Behaviors;
using DevNews.Services.Application.Features.Weather.Queries;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetWeatherForecastByCityQuery).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(GetWeatherForecastByCityQuery).Assembly);

builder.Services.AddWeatherInfrastructure();

builder.Services.AddExceptionHandler<WeatherApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
