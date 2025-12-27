using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMS.Migration.Infrastructure.Data;
using DMS.Migration.Web.ViewModels;
using System.Security.Claims;

namespace DMS.Migration.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        AppDbContext context,
        ILogger<ProfileController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("User ID claim not found or invalid");
                return RedirectToAction("Login", "Account");
            }

            // Fetch user from database
            var user = await _context.Users
                .Include(u => u.Tenant)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return RedirectToAction("Login", "Account");
            }

            // Build view model
            var viewModel = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Username = user.Username,
                TenantName = user.Tenant?.Name,
                Role = user.UserRoles.FirstOrDefault()?.Role?.Name,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,
                FailedLoginAttempts = user.FailedLoginAttempts
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profile");
            TempData["ErrorMessage"] = "An error occurred while loading your profile.";
            return RedirectToAction("Index", "Home");
        }
    }
}
