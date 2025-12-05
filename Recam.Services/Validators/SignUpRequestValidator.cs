using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Validators
{
    public class SignUpRequestValidator: AbstractValidator<SignUpRequest>
    {
        private readonly IdentityOptions _identityOptions;
        public SignUpRequestValidator(IOptions<IdentityOptions> identityOptions)
        {
            _identityOptions = identityOptions.Value;            
            
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("UserName is required.")
                .MaximumLength(50).WithMessage("UserName length must be less than 50.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(_identityOptions.Password.RequiredLength)
                .WithMessage($"Password must be at least {_identityOptions.Password.RequiredLength} characters.");

            // Other email rules
            if (_identityOptions.Password.RequireDigit)
            {
                RuleFor(x => x.Password)
                    .Matches(@"\d").WithMessage("Password must contain at least one number.");
            }

            if (_identityOptions.Password.RequireLowercase)
            {
                RuleFor(x => x.Password)
                    .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.");
            }

            if (_identityOptions.Password.RequireUppercase)
            {
                RuleFor(x => x.Password)
                    .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.");
            }

            if (_identityOptions.Password.RequireNonAlphanumeric)
            {
                RuleFor(x => x.Password)
                    .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");
            }

            RuleFor(x => x.RoleType)
                .NotEmpty().WithMessage("RoleType is required.")
                .Must(r => r == "Agent" || r == "PhotographyCompany")
                .WithMessage("RoleType must be either 'Agent' or 'PhotographyCompany'.");

            When(x => x.RoleType == "Agent", () =>
            {
                RuleFor(x => x.AgentInfo)
                    .NotNull().WithMessage("AgentInfo is required when RoleType = Agent.");

                When(x => x.AgentInfo != null, () =>
                {
                    RuleFor(x => x.AgentInfo.AgentFirstName)
                        .NotEmpty().WithMessage("AgentFirstName is required.")
                        .MaximumLength(50).WithMessage("Name length must be less than 50.");

                    RuleFor(x => x.AgentInfo.AgentLastName)
                        .NotEmpty().WithMessage("AgentLastName is required.")
                        .MaximumLength(50).WithMessage("Name length must be less than 50."); ;

                    RuleFor(x => x.AgentInfo.AgentCompanyName)
                        .NotEmpty().WithMessage("AgentCompanyName is required.")
                        .MaximumLength(50).WithMessage("Name length must be less than 50."); ;
                });
            });

            When(x => x.RoleType == "PhotographyCompany", () =>
            {
                RuleFor(x => x.PhotographyCompanyInfo)
                    .NotNull().WithMessage("PhotographyCompanyInfo is required when RoleType = PhotographyCompany.");

                When(x => x.PhotographyCompanyInfo != null, () =>
                {
                    RuleFor(x => x.PhotographyCompanyInfo.PhotographyCompanyName)
                        .NotEmpty().WithMessage("PhotographyCompanyName is required.")
                        .MaximumLength(50).WithMessage("Name length must be less than 50"); ;
                });
            });
            
        }
    }
}
