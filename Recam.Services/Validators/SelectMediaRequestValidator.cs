using FluentValidation;
using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Validators
{
    public class SelectMediaRequestValidator: AbstractValidator<SelectMediaRequest>
    {
        public SelectMediaRequestValidator()
        {
            RuleFor(x => x)
                .Must(x =>
                {
                    var selectedCount = x.SelectedId?.Count ?? 0;
                    var unselectedCount = x.UnselectedId?.Count ?? 0;

                    return selectedCount + unselectedCount > 0;
                })
                .WithMessage("At least one media asset must be selected or unselected.");

            RuleFor(x => x)
                .Must(x =>
                {
                    var selected = x.SelectedId ?? new List<int>();
                    var unselected = x.UnselectedId ?? new List<int>();

                    return !selected.Intersect(unselected).Any();
                })
                .WithMessage("SelectedId and UnselectedId cannot contain the same media id.");

            RuleForEach(x => x.SelectedId)
                .GreaterThan(0);

            RuleForEach(x => x.UnselectedId)
                .GreaterThan(0);

            RuleFor(x => x.SelectedId)
                .Must(list => list == null || list.Distinct().Count() == list.Count)
                .WithMessage("SelectedId contains duplicates.");

            RuleFor(x => x.UnselectedId)
                .Must(list => list == null || list.Distinct().Count() == list.Count)
                .WithMessage("UnselectedId contains duplicates.");

        }
    }
}
