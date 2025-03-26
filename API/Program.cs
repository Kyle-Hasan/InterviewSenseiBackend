using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using API;
using API.AI;
using API.Auth;
using API.AWS;
using API.Base;
using API.CodeRunner;
using API.Data;
using API.InteractiveInterviewFeedback;
using API.Interviews;
using API.Messages;
using API.MiddleWare;
using API.PDF;
using API.Questions;
using API.Responses;
using API.Users;
using FFMpegCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;
using Microsoft.AspNetCore.Antiforgery;

SetupUploadsFolder();
LoadEnvironment();

var builder = WebApplication.CreateBuilder(args);
ConfigureAppConfiguration(builder);
ConfigureDatabase(builder);
ConfigureKestrel(builder);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

AppConfig.LoadConfiguration(builder.Configuration);

ConfigureServices(builder);

var app = builder.Build();
ConfigurePipeline(app);
app.Run();

static void SetupUploadsFolder()
{
    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
    if (!Directory.Exists(folderPath))
        Directory.CreateDirectory(folderPath);
}

static void LoadEnvironment()
{
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    if (env == "Production")
        Env.Load("backend.env.production");
    else
        Env.Load("backend.env.dev");
}

static void ConfigureAppConfiguration(WebApplicationBuilder builder)
{
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
}

static void ConfigureDatabase(WebApplicationBuilder builder)
{
    var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                           Environment.GetEnvironmentVariable("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options
            .UseLazyLoadingProxies()
            .UseNpgsql(connectionString)
    );
}

static void ConfigureKestrel(WebApplicationBuilder builder)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 2000000000;
    });
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddSignalR();
    builder.Services.AddIdentity<AppUser, AppRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    var secretKey = Encoding.UTF8.GetBytes(JwtSettings.SecretKey);
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("accessToken", out string accessToken))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var token = context.SecurityToken as JsonWebToken;
                var tokenTypeClaim = token.Claims.FirstOrDefault(c => c.Type == "tokenType");
                if (tokenTypeClaim == null || tokenTypeClaim.Value != "access")
                    context.Fail("Invalid token type");
            }
        };
    });

    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IOpenAIService, GeminiService>();
    builder.Services.AddScoped<IinterviewRepository, InterviewRepository>();
    builder.Services.AddScoped<IinterviewService, InterviewService>();
    builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
    builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
    builder.Services.AddScoped<IResponseService, ResponseService>();
    builder.Services.AddScoped<IQuestionService, QuestionService>();
    builder.Services.AddScoped<IBlobStorageService, S3Service>();
    builder.Services.AddScoped<IFileService, FileService>();
    builder.Services.AddScoped<IMessageRepository, MessageRepository>();
    builder.Services.AddScoped<IinterviewFeedbackService, InterviewFeedbackService>();
    builder.Services.AddScoped<ICodeRunnerService, JudgeZeroService>();
    builder.Services.AddScoped<CurrentUserFilter>();
    builder.Services.AddScoped<CsrfValidationFilter>();
    builder.Services.AddScoped<IMessageService, MessageService>();
    builder.Services.AddSingleton<IdToMessage>();
    builder.Services.AddScoped<IinterviewFeedbackRepository, InterviewFeedbackRepository>();
    builder.Services.AddScoped<ICodeSubmissionRepository, CodeSubmissionRepository>();
    builder.Services.AddHttpClient<IOpenAIService, GeminiService>();
    builder.Services.AddAntiforgery(options =>
    {
        
        options.HeaderName = "X-CSRF-TOKEN";
    });
    builder.Services.AddAuthorization();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

static void ConfigurePipeline(WebApplication app)
{
    // csrf protection, put on all cookies
    app.Use(async (context, next) =>
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("CSRF-TOKEN", tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,  // readable by JavaScript so the client can attach it in requests
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        await next();
    });


    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors(x => x.AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials()
                           .WithOrigins("http://localhost:3000", "https://localhost:3000", "http://localhost:3000/"));
    }
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<InterviewHub>("/hubs/interview");
}
