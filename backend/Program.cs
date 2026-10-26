using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.Services;

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ReDI.Assessment.Api.Settings;

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
builder.Services.AddScoped<OpenAnswerReviewService>();
builder.Services.AddScoped<FrontendQuestionsResponseService>();
builder.Services.AddScoped<FrontendSubmissionService>();
builder.Services.AddScoped<FrontendAssessmentSummaryService>();
builder.Services.AddScoped<FrontendAssessmentReadinessService>();
builder.Services.AddScoped<AssessmentStructureGuardService>();
builder.Services.AddScoped<AssessmentContentService>();

builder.Services.AddOpenApi();

builder.Services.Configure<AdminAuthSettings>(
    builder.Configuration.GetSection("AdminAuth")
);

var adminAuthSettings = builder.Configuration
    .GetSection("AdminAuth")
    .Get<AdminAuthSettings>() ?? new AdminAuthSettings();

var jwtSecretBytes = Encoding.UTF8.GetBytes(adminAuthSettings.JwtSecret);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = adminAuthSettings.Issuer,
            ValidAudience = adminAuthSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSecretBytes)
        };
    });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();