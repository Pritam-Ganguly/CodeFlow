using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlow.Web.Controllers
{
    [Authorize]
    public class VotesController : Controller
    {
        private readonly IVoteRepository _voteRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public VotesController(IVoteRepository voteRepository, UserManager<ApplicationUser> userManager)
        {
            _voteRepository = voteRepository;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Question(int questionId, int voteType)
        {
            if(voteType != 1 && voteType != -1)
            {
                return BadRequest("Invalid vote type.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return BadRequest("Invalid User.");
            }

            var vote = new Vote
            {
                VoteType = voteType,
                UserId = user.Id,
                Questionid = questionId,
            };

            var success = await _voteRepository.AddVoteAsync(vote);
            if (!success)
            {
                return BadRequest("Could not process vote.");
            }

            var newScore = await _voteRepository.GetScoreForQuestionAsync(questionId);
            return Ok( new {newScore});
        }

        [HttpPost]
        public async Task<IActionResult> Answer(int answerId, int voteType)
        {
            if (voteType != 1 && voteType != -1)
            {
                return BadRequest("Invalid vote type.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("Invalid User.");
            }

            var vote = new Vote
            {
                VoteType = voteType,
                UserId = user.Id,
                AnswerId = answerId,
            };

            var success = await _voteRepository.AddVoteAsync(vote);
            if (!success)
            {
                return BadRequest("Could not process vote.");
            }

            var newScore = await _voteRepository.GetScoreForAnswerAsync(answerId);
            return Ok(new { newScore });
        }
    }
}
