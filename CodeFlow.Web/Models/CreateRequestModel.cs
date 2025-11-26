using FluentValidation;
using System.ComponentModel;

namespace CodeFlow.Web.Models
{
    public class CreateRequestModel
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        
        [DisplayName("Tags")]
        public string TagsInput { get;set; } = string.Empty;
    }

    public class CreateRequestValidator : AbstractValidator<CreateRequestModel>
    {
        public CreateRequestValidator()
        {
            RuleFor(r => r.Title)
                .NotEmpty().WithMessage("Question title cannot be empty")
                .MinimumLength(10).WithMessage("Question title is to short")
                .MaximumLength(200).WithMessage("Question title size limit reached");

            RuleFor(r => r.Body)
                .NotEmpty().WithMessage("Question body cannot be empty")
                .MinimumLength(10).WithMessage("Quesiton body is to short")
                .MaximumLength(2000).WithMessage("Question body size limit reached");

            RuleFor(r => r.TagsInput)
                .NotEmpty().WithMessage("Please add atleast one tag")
                .MaximumLength(100).WithMessage("Maximum limit reached for number of tags");
        }
    }

}
