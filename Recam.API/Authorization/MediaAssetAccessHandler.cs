using Microsoft.AspNetCore.Authorization;
using Recam.Models.Entities;
using System.Security.Claims;

namespace Recam.API.Authorization
{
    public class MediaAssetAccessHandler : AuthorizationHandler<MediaAssetAccessRequirement, MediaAsset>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MediaAssetAccessRequirement requirement, MediaAsset resource)
        {
            // Check user's authentication status
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                return Task.CompletedTask;
            }

            // Get user Id and role from claims
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
                    resource.ListingCase.AgentListingCases != null &&
                    resource.ListingCase.AgentListingCases.Any(al => 
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
