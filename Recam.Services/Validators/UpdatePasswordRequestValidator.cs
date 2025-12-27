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
    public class UpdatePasswordRequestValidator: AbstractValidator<UpdatePasswordRequest>
    {
        private readonly IdentityOptions _identityOptions;

        public UpdatePasswordRequestValidator(IOptions<IdentityOptions> identityOptions)
        {
            _identityOptions = identityOptions.Value;

            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("CurrentPassword is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("NewPassword is required.")
                .MinimumLength(_identityOptions.Password.RequiredLength)
                .WithMessage($"Password must be at least {_identityOptions.Password.RequiredLength} characters.");

            RuleFor(x => x)
                .Must(x => x.CurrentPassword != x.NewPassword)
                .WithMessage("New password must be different from the current password.");

            // Other password rules
            if (_identityOptions.Password.RequireDigit)
            {
                RuleFor(x => x.NewPassword)
                    .Matches(@"\d").WithMessage("Password must contain at least one number.");
            }

            if (_identityOptions.Password.RequireLowercase)
            {
                RuleFor(x => x.NewPassword)
                    .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.");
            }

            if (_identityOptions.Password.RequireUppercase)
            {
                RuleFor(x => x.NewPassword)
                    .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.");
            }

            if (_identityOptions.Password.RequireNonAlphanumeric)
            {
                RuleFor(x => x.NewPassword)
                    .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");
            }
        }
    }
}
