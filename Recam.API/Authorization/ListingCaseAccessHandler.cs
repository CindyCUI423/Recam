using Microsoft.AspNetCore.Authorization;
using Recam.Models.Entities;
using System.Security.Claims;

namespace Recam.API.Authorization
{
    /// <summary>
    /// Handles authorization requirements for accessing a listing case based on the user's identity and role.
    /// </summary>
    public class ListingCaseAccessHandler: AuthorizationHandler<ListingCaseAccessRequirement, ListingCase>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ListingCaseAccessRequirement requirement, ListingCase resource)
        {
            // Check user's authentication status
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                return Task.CompletedTask;
            }

            // Get user ID and role from claims
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = context.User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                return Task.CompletedTask;
            }

            var canAccess = role switch
            {
                "PhotographyCompany" =>
                    resource.UserId == userId,

                "Agent" => 
                    resource.AgentListingCases != null &&
                    resource.AgentListingCases.Any(al =>
                        al.Agent != null &&
                        al.Agent.Id == userId),

                _ => false
            };

            if (canAccess)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
