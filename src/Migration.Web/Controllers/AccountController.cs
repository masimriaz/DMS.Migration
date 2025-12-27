using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DMS.Migration.Application.Interfaces;
using DMS.Migration.Web.ViewModels;
using DMS.Migration.Web.Extensions;

namespace DMS.Migration.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthService authService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already authenticated, redirect to home
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Validate credentials
            var user = await _authService.ValidateCredentialsAsync(model.Email, model.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                TempData.AddToast("error", "Invalid Credentials", "The email or password you entered is incorrect.");
                return View(model);
            }

            // Build claims principal
            var principal = _authService.BuildClaimsPrincipal(user);

            // Sign in with cookie
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddMinutes(30),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            // Update last login timestamp
            await _authService.UpdateLastLoginAsync(user.Id);

            _logger.LogInformation("User {Email} logged in successfully", model.Email);

            TempData.AddToast("success", "Welcome!", $"You have successfully logged in as {user.FullName}.");

            return RedirectToLocal(model.ReturnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
            TempData.AddToast("error", "Login Error", "An unexpected error occurred. Please try again.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name ?? "User";

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User {UserName} logged out", userName);

        TempData.AddToast("info", "Logged Out", "You have been successfully logged out.");

        return RedirectToAction("Login", "Account");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
