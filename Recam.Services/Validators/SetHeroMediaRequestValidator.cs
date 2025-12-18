using FluentValidation;
using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Validators
{
    public class SetHeroMediaRequestValidator: AbstractValidator<SetHeroMediaRequest>
    {
        public SetHeroMediaRequestValidator()
        {
            RuleFor(x => x.MediaAssetId)
                .NotNull()
                .NotEmpty()
                .WithMessage("Media Asset Id is required.");
        }
    }
}
