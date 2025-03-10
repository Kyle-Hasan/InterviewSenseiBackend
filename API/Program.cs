using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using API;
using API.AI;
using API.Auth;
using API.AWS;
using API.Data;
using API.Interviews;
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

var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

// Ensure the folder exists
if (!Directory.Exists(folderPath))
{
    Directory.CreateDirectory(folderPath);
}
// Determine the environment
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

// Load the corresponding .env file
if (env == "Production")
{
    Env.Load("backend.env.production");
}
else
{
    Env.Load("backend.env.dev");
}



var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                       Environment.GetEnvironmentVariable("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
// Add services to the container.


AppConfig.LoadConfiguration(builder.Configuration);


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
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events= new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("accessToken", out string accessToken))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var token = context.SecurityToken as JsonWebToken;
            var tokenTypeClaim  = token.Claims.FirstOrDefault(c => c.Type == "tokenType");
            if(tokenTypeClaim == null || tokenTypeClaim.Value != "access") {
                context.Fail("Invalid token type");
            }
        }
    };

});

builder.Services.AddScoped<IJwtTokenService,JwtTokenService>();
builder.Services.AddScoped<IOpenAIService, GeminiService>();
builder.Services.AddScoped<IinterviewRepository, interviewRepository>();
builder.Services.AddScoped<IinterviewService,InterviewService>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IResponseRepository,ResponseRepository>();
builder.Services.AddScoped<IResponseService,ResponseService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IBlobStorageService, S3Service>();

builder.Services.AddHttpClient<IOpenAIService, GeminiService>();


builder.Services.AddAuthorization();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(x=> x.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins("http://localhost:3000","https://localhost:3000","http://localhost:3000/"));




app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<InterviewHub>("/hubs/interview");

app.Run();
