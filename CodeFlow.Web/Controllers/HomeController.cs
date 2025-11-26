using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using CodeFlow.Web.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
        try
        {

            HomeViewModel model = new HomeViewModel();
            if (!string.IsNullOrWhiteSpace(search))
            {
                _logger.LogInformation("Search query executed: {SearchQuery}", search);

                questions = await _questionRepository.SearchAsync(search);

                ViewData["SearchQuery"] = search;
                model.IsSearchResult = true;
                model.SearchTerm = search;
            }
            else
            {
                _logger.LogInformation("Fetching all questions");
                questions = await _questionRepository.GetRecentWithTagsAsync();
            }
            model.Questions = questions;

            _logger.LogInformation("Search completed succesfully");

            return View(model);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occured while searching question using {SerachQuery}", search);

            HomeViewModel errorModel = new HomeViewModel()
            {
                HasError = true,
                ErrorMessage = $"An error occured: {ex.Message}"
            };
            return View(errorModel);   
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int statusCode = 500)
    {
        string RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var statusCodeFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        var errorModel = new ErrorViewModel()
        {
            RequestId = RequestId,
            StatusCode = statusCode
        };

        if (statusCodeFeature != null)
        {
            var original = statusCodeFeature.OriginalPath ?? string.Empty;
            var query = statusCodeFeature.OriginalQueryString ?? string.Empty;
            errorModel.OriginalPath = original + query;
            _logger.LogWarning("Returning status code page {StatusCode} for original path {OriginalPath}{OriginalQuery}", statusCode, original, query);
        }
        if (exceptionHandlerPathFeature?.Error != null)
        {
            _logger.LogError(exceptionHandlerPathFeature.Error, "Error occured processing request {RequestId} for path {Path}", RequestId, exceptionHandlerPathFeature.Path);
            errorModel.OriginalPath = exceptionHandlerPathFeature.Path;
        }

        return View(errorModel);
    }
}
