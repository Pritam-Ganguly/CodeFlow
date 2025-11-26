using CodeFlow.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlow.Web.Components
{
    public class AnswersViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(IEnumerable<AnswerViewModel> answers, bool isAuthor)
        {
            var orderedAnswers = answers.OrderByDescending(a => a.IsAccepted).OrderByDescending(a => a.CreatedAt);

            var answerViewModel = new AnswerViewComponentModel()
            {
                Answers = orderedAnswers,
                IsAuthor = isAuthor
            };

            return View(answerViewModel);
        }
    }
}
