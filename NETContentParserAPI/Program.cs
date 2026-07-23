using System.Text.Json;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IContentDecoder, Base64ContentDecoder>();
builder.Services.AddSingleton<IContentParser, CsvContentParser>();
builder.Services.AddSingleton<IContentParser, InternalJsonContentParser>();
builder.Services.AddSingleton<ContentParserFactory>();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;

        if (exception != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exception, "Unhandled exception occurred while processing request.");
        }

        var (statusCode, message) = exception switch
        {
            BadHttpRequestException badRequestEx
                when badRequestEx.InnerException is JsonException =>
            (
                StatusCodes.Status400BadRequest,
                "Invalid JSON format."
            ),
            ArgumentException ex =>
            (
                StatusCodes.Status400BadRequest,
                ex.Message
            ),
            JsonException =>
            (
                StatusCodes.Status400BadRequest,
                "Invalid JSON format."
            ),
            FormatException =>
            (
                StatusCodes.Status400BadRequest,
                "Invalid Base64 string in content field."
            ),
            NotSupportedException ex =>
            (
                StatusCodes.Status400BadRequest,
                ex.Message
            ),
            BadHttpRequestException =>
            (
                StatusCodes.Status400BadRequest,
                "Invalid request body."
            ),
            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred."
            )
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(ParseResponse<object>.Failure(message));
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

var apiV1 = app.MapGroup("/api/v1");

apiV1.MapPost("/parse-content", (
    [FromBody] ParseRequest request,
    [FromServices] IContentDecoder decoder,
    [FromServices] ContentParserFactory parserFactory) =>
{
    string rawContent = decoder.DecodeBase64(request.Content);
    var parser = parserFactory.GetParser(request.Type);
    var result = parser.Parse(rawContent);

    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(ParseResponse<object>.Failure(result.ErrorMessage!));
    }

    return Results.Ok(ParseResponse<object>.Success(result.RawData!, result.Count));
})
.Accepts<ParseRequest>("application/json")
.Produces<ParseResponse<object>>(StatusCodes.Status200OK)
.Produces<ParseResponse<object>>(StatusCodes.Status400BadRequest)
.Produces<ParseResponse<object>>(StatusCodes.Status422UnprocessableEntity);

app.Run();