using FluentValidation;

namespace CodeFlow.Web.Models
{
    public class EditRequestModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class EditRequestModelValidator : AbstractValidator<EditRequestModel>
    {
        public EditRequestModelValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title Cannot be empty")
                .MinimumLength(10).WithMessage("Title is too short")
                .MaximumLength(500).WithMessage("Maximum limit reached for title length");
            RuleFor(x => x.Body).NotEmpty().WithMessage("Body cannot be empty")
                .MinimumLength(10).WithMessage("Body is too short")
                .MaximumLength(5000).WithMessage("Maximum limit reached for body length");
        }
    }
}
