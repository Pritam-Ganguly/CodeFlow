using CodeFlow.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlow.Web.Components
{
    public class AnswersViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<AnswerViewModel> answers, bool isAuthor)
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
