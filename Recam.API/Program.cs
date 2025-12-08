using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Recam.Common.Exceptions;
using Recam.DataAccess.Collections;
using Recam.DataAccess.Data;
using Recam.DataAccess.Seeders;
using Recam.Models.Entities;
using Recam.Models.Settings;
using Recam.Repositories.Interfaces;
using Recam.Repositories.Repositories;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using Recam.Services.Mappers;
using Recam.Services.Services;
using Recam.Services.Validators;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recam.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {

                options.InvalidModelStateResponseFactory = context =>
                {
                    // Get all the errors form ModelState
                    var errors = context.ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );

                    var errorResponse = new ErrorResponse(
                        statusCode: StatusCodes.Status400BadRequest,
                        message: "One or more validation errors occurred. Please check your input.",
                        errorType: "ValidationError")
                    {
                        Errors = errors
                    };

                    return new ObjectResult(errorResponse)
                    {
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                };
            })
            .AddJsonOptions(options =>
            {
                // Configure enum converter --> convert enum to string
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "Jwt Authorization",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "bearer"
            });
            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
            var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
            options.IncludeXmlComments(xmlPath);
        });

        // Register SQL Server
        builder.Services.AddDbContext<RecamDbContext>(
            options => options.UseSqlServer(builder.Configuration.GetConnectionString("RECAM-SQLSERVER"))
        );

        // Register MongoDB
        builder.Services.Configure<MongoDbSettings>(
            options => {
                options.ConnectionStrings = builder.Configuration["ConnectionStrings:MongoDb"];
                options.DatabaseName = builder.Configuration["DatabaseSettings:DatabaseName"];
            }
        );

        builder.Services.AddSingleton<MongoDbContext>();

        // Regiester Identity, UserManager
        builder.Services.AddIdentity<User, Role>(options =>
        {
            // Configure user signup password rules
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            // Configure user rules
            options.User.RequireUniqueEmail = true;
        })
            .AddEntityFrameworkStores<RecamDbContext>()
            .AddDefaultTokenProviders();
        

        // Register Services and Repositories
        builder.Services.AddScoped<IAuthRepository, AuthRepository>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddSingleton<IUserActivityLogRepository, UserActivityLogRepository>();
        builder.Services.AddSingleton<ICaseHistoryRepository, CaseHistoryRepository>();
        builder.Services.AddScoped<IListingCaseRepository, ListingCaseRepository>();
        builder.Services.AddScoped<IListingCaseService, ListingCaseService>();


        // Register UnitOfWork to handle transaction
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register global exception handler
        builder.Services.AddSingleton<GlobalExceptionHandler>();

        // Register AutoMapper
        builder.Services.AddAutoMapper(typeof(MappingProfile));

        // Register FluentValidator
        builder.Services.AddValidatorsFromAssemblyContaining<SignUpRequestValidator>();
        builder.Services.AddFluentValidationAutoValidation();


        // Register Authorization - assign JWT
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("PhotographyCompanyPolicy",
                policy => policy.RequireClaim(ClaimTypes.Role, "PhotographyCompany"));
            options.AddPolicy("AgentPolicy",
                policy => policy.RequireClaim(ClaimTypes.Role, "Agent"));
        });

        // Register Authentication - verify JWT
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };

            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();

                    var response = context.Response;
                    response.StatusCode = StatusCodes.Status401Unauthorized;
                    response.ContentType = "application/json";

                    var error = new ErrorResponse(
                        statusCode: StatusCodes.Status401Unauthorized,
                        message: "Unauthorized. Please provide a valid access token.",
                        errorType: "AuthorizationError");

                    var json = JsonSerializer.Serialize(error);
                    await response.WriteAsync(json);
                },
                OnForbidden = async context =>
                {
                    var response = context.Response;
                    response.StatusCode = StatusCodes.Status403Forbidden;
                    response.ContentType = "application/json";

                    var error = new ErrorResponse(
                        statusCode: StatusCodes.Status403Forbidden,
                        message: "Forbidden. You don't have permission to access this resource.",
                        errorType: "AuthorizationError");

                    var json = JsonSerializer.Serialize(error);
                    await response.WriteAsync(json);
                }
            };
        });

        var app = builder.Build();

        // Activate global exception handler by adding the middleware
        app.UseExceptionHandler(
            errorApp => 
            {
                errorApp.Run(async context =>
                {
                    var exceptionHandler = context.RequestServices.GetRequiredService<GlobalExceptionHandler>();
                    await exceptionHandler.HandleException(context);
                }
                );
            }
        );

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Call seeder
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            await RecamDbSeeder.SeedAsync(services);
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
