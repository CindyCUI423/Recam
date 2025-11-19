
using Microsoft.EntityFrameworkCore;
using Recam.DataAccess.Data;
using Recam.Models.Settings;

namespace Recam.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Register SQL Server DB
        builder.Services.AddDbContext<RecamDbContext>(
            options => options.UseSqlServer(builder.Configuration.GetConnectionString("RECAM-SQLSERVER"))
        );

        builder.Services.Configure<MongoDbSettings>(
            options => {
                options.ConnectionStrings = builder.Configuration["ConfigurationStrings:MongoDb"];
                options.DatabaseName = builder.Configuration["DatabaseSettings:DatabaseName"];
            }
        );

        builder.Services.AddSingleton<MongoDbContext>();
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
