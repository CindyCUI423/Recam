using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.DataAccess.Data
{
    public class RecamDbContextFactory : IDesignTimeDbContextFactory<RecamDbContext>
    {
        public RecamDbContext CreateDbContext(string[] args)
        {
            var optionBuilder = new DbContextOptionsBuilder<RecamDbContext>();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Recam.API"))
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("RECAM-SQLSERVER");

            optionBuilder.UseSqlServer(connectionString);

            return new RecamDbContext(optionBuilder.Options);
        }
    }
}
