using CodeFlow.core.Models;
using CodeFlow.core.Repositories;
using CodeFlow.Web.Filters;
using CodeFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CodeFlow.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserRepository userRepository, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [RedirectIfAuthenticated]
        [LogAction]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;  
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ValidateModel]
        [LogAction]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl)
        {
            returnUrl ??= Url.Content("~/");

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var user = new ApplicationUser()
                {
                    UserName = model.DisplayName?.Trim(),
                    Email = model.Email?.Trim().ToLower(),
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                
                stopwatch.Stop();

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserName} (ID: {UserId}) created a new account with password. Registration took {ElapsedMs}ms", 
                        user.UserName, user.Id, stopwatch.ElapsedMilliseconds);

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    _logger.LogInformation("User {UserName} (ID: {UserId}) was automatically signed in after registration", user.UserName, user.Id);
                    TempData["SuccessMessage"] = "Account created successfully: Welcome to CodeFlow.";

                    var profile = await _userRepository.CreateUserProfile(user.Id);

                    if(profile == null)
                    {
                        throw new Exception($"Error creating profile for UserId {user.Id}");
                    }

                    return LocalRedirect(returnUrl);
                }

                foreach(var error in result.Errors)
                {
                    switch (error.Code)
                    {
                        case "DuplicateUserName":
                            ModelState.AddModelError(nameof(model.DisplayName), "This display name is already taken, Please choose another");
                            break;
                        case "DuplicateEmail":
                            ModelState.AddModelError(nameof(model.Email), "This email address is already registered");
                            break;
                        case "DuplicateUser":
                            ModelState.AddModelError(string.Empty, "User already exists, please log in.");
                            break;
                        default: break;
                    }
                }

                _logger.LogWarning("User registration failed for {Email}, Errors: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering user with email {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occured while creating your account. Please try again.");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        [RedirectIfAuthenticated]
        [LogAction]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ValidateModel]
        [LogAction]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            try
            {
                var stopwatch = Stopwatch.StartNew();

                await _signInManager.SignOutAsync();

                var normalizedUserName = _userManager.NormalizeName(model.DisplayName);
                var user = await _userManager.FindByNameAsync(normalizedUserName);

                if(user == null)
                {
                    _logger.LogWarning("Invalid login attempt for user {UserName} for IP {RemoteIpAddress}", model.DisplayName, HttpContext.Connection.RemoteIpAddress);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.DisplayName, model.Password, model.RememberMe, lockoutOnFailure: false);

                stopwatch.Stop();

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserName}, (Id: {UserId}) logged in successfully from Ip {RemoteAddress}. Login took {ElapsedMs}ms",
                        user.UserName, user.Id, HttpContext.Connection.RemoteIpAddress, stopwatch.ElapsedMilliseconds);
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for user {UserName} from IP {RemoteIdAddress}. Result {SignInResult}",
                        model.DisplayName, HttpContext.Connection.RemoteIpAddress, result.ToString());

                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login attempt for user {UserName}", model.DisplayName);
                ModelState.AddModelError(string.Empty, "An error occurred while processing your login. Please try again");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [LogAction]
        public async Task<ActionResult> Logout()
        {
            var userName = User.Identity!.Name;
            var userId = _userManager.GetUserId(User);

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {UserName} (ID: {UserId}) logged out", userName, userId);

            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [LogAction]
        public IActionResult AccessDenied([FromQuery] string? returnUrl)
        {
            var accessModel = new AccessDeniedViewModel()
            {
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
            };
            return View(accessModel);
        }
    }
}
