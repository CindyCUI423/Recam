using FluentValidation;
using Recam.Models.Enums;
using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Validators
{
    public class CreateMediaAssetsBatchRequestValidator: AbstractValidator<CreateMediaAssetsBatchRequest>
    {
        public CreateMediaAssetsBatchRequestValidator()
        {
            RuleFor(x => x.MediaType)
                .IsInEnum()
                .WithMessage("MediaType must be once of: Photo, Video, FloorPlan, VRTour.");

            RuleFor(x => x.MediaUrls)
                .NotNull()
                .NotEmpty()
                .WithMessage("MediaUrl is required.");

            RuleForEach(x => x.MediaUrls)
                .NotEmpty()
                .WithMessage("MediaUrl cannot be empty.")
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("MediaUrl must be a valid absolute URL.");

            RuleFor(x => x)
                .Must(x => x.MediaType == MediaType.Photo || x.MediaUrls.Count == 1)
                .WithMessage("Only photo media type allows multiple MediaUrls.");
        }
    }
}
