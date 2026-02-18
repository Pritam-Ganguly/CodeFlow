using CodeFlow.core.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CodeFlow.Web.Models
{
    public class ReportViewModel
    {
        public int PostId { get; set; }
        public FlagPostType PostType { get; set; }

        public string? PostTitle { get; set; }
        public string PostBody { get; set; } = string.Empty;

        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;

        public IEnumerable<SelectListItem> FlagSelectList { get; set; } = [];
        public int SelectedFlagTypeId { get; set; }

        public string Reason { get; set; } = string.Empty;
    }

    public class ReportViewModelValidator : AbstractValidator<ReportViewModel>
    {
        public ReportViewModelValidator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason cannot be null or empty..")
                .MinimumLength(10).WithMessage("Reason is too short..")
                .MaximumLength(500).WithMessage("Maximum length is reached..");
        }
    }
}
