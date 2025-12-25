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
            await SeedDefaultUsers(userManager, context);
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

        private static async Task SeedDefaultUsers(UserManager<User> userManager, RecamDbContext context)
        {
            const string pcEmail = "photography@example.com";
            const string agentEmail = "agent@example.com";
            const string pcUserName = "photography_admin";
            const string agentUserName = "agent_user";
            const string password = "12345Abcde!"; //TODO: Hardcoded password for now (change strategy when connected to cloud)
            const string photographyCompanyName = "Default Photography Company";
            const string agentFistName = "AF";
            const string agentLastName = "AL";
            const string agentCompanyName = "Default Agent Company";

            // Check user's existance
            var existingPC = await userManager.FindByEmailAsync(pcEmail);
            var existingAgent = await userManager.FindByEmailAsync(agentEmail);

            if (existingPC != null)
            {
                // Check user role (PhotographyCompany) existancec
                if (!await userManager.IsInRoleAsync(existingPC, "PhotographyCompany"))
                {
                    await userManager.AddToRoleAsync(existingPC, "PhotographyCompany");
                }

                // Create PhotographyCompanyInfo if not existed
                var existingPhotoCompany = await context.PhotographyCompanies.FirstOrDefaultAsync(p => p.Id == existingPC.Id);
                if (existingPhotoCompany == null)
                {
                    context.PhotographyCompanies.Add( new PhotographyCompany 
                    {
                        Id = existingPC.Id,
                        PhotographyCompanyName = photographyCompanyName
                    });

                    await context.SaveChangesAsync();
                }

                return;
            }

            if (existingAgent != null)
            {
                // Check user role (Agent) existancec
                if (!await userManager.IsInRoleAsync(existingAgent, "Agent"))
                {
                    await userManager.AddToRoleAsync(existingAgent, "Agent");
                }

                // Create AgentInfo if not existed
                var existingAgentCompany = await context.Agents.FirstOrDefaultAsync(a => a.Id == existingAgent.Id);
                if (existingAgentCompany == null)
                {
                    context.Agents.Add(new Agent
                    {
                        Id = existingAgent.Id,
                        AgentFirstName = agentFistName,
                        AgentLastName = agentLastName,
                        CompanyName = agentCompanyName
                    });

                    await context.SaveChangesAsync();
                }

                return;
            }

            // Create users
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var pcUser = new User
                {
                    UserName = pcUserName,
                    Email = pcEmail,
                    EmailConfirmed = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                };

                var agentUser = new User
                {
                    UserName = agentUserName,
                    Email = agentEmail,
                    EmailConfirmed = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                };

                // Create PhotographyCompany user
                var createPCUserResult = await userManager.CreateAsync(pcUser, password);
                if (!createPCUserResult.Succeeded)
                {
                    var message = string.Join(";", createPCUserResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create default user {pcUserName}: {message}.");
                }

                var addPCToRoleResult = await userManager.AddToRoleAsync(pcUser, "PhotographyCompany");
                if (!addPCToRoleResult.Succeeded)
                {
                    var message = string.Join(";", addPCToRoleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to assign Photography role to defult user {pcUserName}: {message}.");
                }

                context.PhotographyCompanies.Add(new PhotographyCompany
                {
                    Id = pcUser.Id,
                    PhotographyCompanyName = photographyCompanyName,
                });

                // Create Agent user
                var createAgentUserResult = await userManager.CreateAsync(agentUser, password);
                if (!createAgentUserResult.Succeeded)
                {
                    var message = string.Join(";", createAgentUserResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create default user {agentUserName}: {message}.");
                }

                var addAgentToRoleResult = await userManager.AddToRoleAsync(agentUser, "Agent");    
                if (!addAgentToRoleResult.Succeeded)
                {
                    var message = string.Join(";", addAgentToRoleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to assign Agent role to defult user {agentUserName}: {message}.");
                }

                context.Agents.Add(new Agent
                {
                    Id = agentUser.Id,
                    AgentFirstName = agentFistName,
                    AgentLastName = agentLastName,
                    CompanyName = agentCompanyName,
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
