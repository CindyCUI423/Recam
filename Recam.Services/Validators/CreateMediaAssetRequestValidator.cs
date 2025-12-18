using FluentValidation;
using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Validators
{
    public class CreateMediaAssetRequestValidator: AbstractValidator<CreateMediaAssetRequest>
    {
        public CreateMediaAssetRequestValidator()
        {
            RuleFor(x => x.MediaType)
                .IsInEnum()
                .WithMessage("MediaType must be once of: Photo, Video, FloorPlan, VRTour.");

            RuleFor(x => x.MediaUrl)
                .NotEmpty()
                .WithMessage("MediaUrl is required.")
                .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                .WithMessage("MediaUrl must be a valid absolute URL.");
        }
    }
}
