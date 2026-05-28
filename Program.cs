using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string DevelopmentCorsPolicy = "DevelopmentCorsPolicy";

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevelopmentCorsPolicy, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AssessmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AttemptStatusService>();

builder.Services.AddScoped<SubmissionResponseService>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ReDI Assessment API v1");
    });
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevelopmentCorsPolicy);
}

app.UseAuthorization();

app.MapControllers();

app.Run();