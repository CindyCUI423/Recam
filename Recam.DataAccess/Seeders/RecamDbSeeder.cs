using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recam.DataAccess.Data;
using Recam.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.DataAccess.Seeders
{
    public class RecamDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var context = provider.GetRequiredService<RecamDbContext>();
            var userManager = provider.GetRequiredService<UserManager<User>>();
            var roleManager = provider.GetRequiredService<RoleManager<Role>>();

            await context.Database.MigrateAsync();

            // Seed user role types
            await SeedRolesAsync(roleManager);

            // Seed default user data
            await SeedDefaultUser(userManager, context);
        }

        private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
        {
            var roles = new[] { "Agent", "PhotographyCompany" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new Role
                    {
                        Name = roleName
                    };

                    var result = await roleManager.CreateAsync(role);

                    if (!result.Succeeded)
                    {
                        var message = string.Join(";", result.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to seed role '{roleName}': {message}");   
                    }
                }


            }
        }

        private static async Task SeedDefaultUser(UserManager<User> userManager, RecamDbContext context)
        {
            const string email = "example@example.com";
            const string userName = "photography_admin";
            const string password = "12345Abc!"; //TODO: Hardcoded password for now (change strategy when connected to cloud)
            const string photographyCompanyName = "Default Photography Company";

            // Check user's existance
            var existingUser = await userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                // Check user role (PhotographyCompany) existancec
                if (!await userManager.IsInRoleAsync(existingUser, "PhotographyCompany"))
                {
                    await userManager.AddToRoleAsync(existingUser, "PhotographyCompany");
                }

                // Create PhotographyCompanyInfo if not existed
                var existingPhotoCompany = await context.PhotographyCompanies.FirstOrDefaultAsync(p => p.Id == existingUser.Id);
                if (existingPhotoCompany == null)
                {
                    context.PhotographyCompanies.Add( new PhotographyCompany 
                    {
                        Id = existingUser.Id,
                        PhotographyCompanyName = photographyCompanyName
                    });

                    await context.SaveChangesAsync();
                }

                return;
            }

            // Create user
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var user = new User
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                };

                var createUserResult = await userManager.CreateAsync(user, password);
                if (!createUserResult.Succeeded)
                {
                    var message = string.Join(";", createUserResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create default user {userName}: {message}.");
                }

                var addToRoleResult = await userManager.AddToRoleAsync(user, "PhotographyCompany");
                if (!addToRoleResult.Succeeded)
                {
                    var message = string.Join(";", addToRoleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to assign Photography role to defult user {userName}: {message}.");
                }

                context.PhotographyCompanies.Add(new PhotographyCompany
                {
                    Id = user.Id,
                    PhotographyCompanyName = photographyCompanyName,
                });
                await context.SaveChangesAsync();

                await transaction.CommitAsync();

            }
            catch
            {
               await transaction.RollbackAsync();
               throw;
            }
 
        }
    }
}
