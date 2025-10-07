using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CodeFlow.Web.Models;
using CodeFlow.core.Models;
using CodeFlow.core.Repositories;

namespace CodeFlow.Web.Controllers;

public class HomeController : Controller
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IQuestionRepository questionRepository,ILogger<HomeController> logger)
    {
        _questionRepository = questionRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string search)
    {
        IEnumerable<Question> questions;

        if (!string.IsNullOrWhiteSpace(search))
        {
            questions = await _questionRepository.SearchAsync(search);
            ViewData["SearchQuery"] = search;
        }
        else
        {
            questions = await _questionRepository.GetRecentWithTagsAsync();
        }

        return View(questions);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
