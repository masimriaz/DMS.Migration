using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DMS.Migration.Application.Common.Abstractions;
using DMS.Migration.Application.Connections.Commands;
using DMS.Migration.Application.Connections.Queries;
using DMS.Migration.Application.Connections.DTOs;
using DMS.Migration.Application.Common.Models;
using DMS.Migration.Web.ViewModels.Connections;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Web.Controllers;

/// <summary>
/// Controller for managing connections (refactored to use Clean Architecture)
/// </summary>
[Authorize]
public class ConnectionsController : Controller
{
    private readonly ICommandHandler<CreateConnectionCommand, ConnectionDto> _createConnectionHandler;
    private readonly ICommandHandler<VerifyConnectionCommand, VerificationResultDto> _verifyConnectionHandler;
    private readonly IQueryHandler<GetConnectionByIdQuery, ConnectionDto> _getConnectionHandler;
    private readonly ILogger<ConnectionsController> _logger;

    public ConnectionsController(
        ICommandHandler<CreateConnectionCommand, ConnectionDto> createConnectionHandler,
        ICommandHandler<VerifyConnectionCommand, VerificationResultDto> verifyConnectionHandler,
        IQueryHandler<GetConnectionByIdQuery, ConnectionDto> getConnectionHandler,
        ILogger<ConnectionsController> logger)
    {
        _createConnectionHandler = createConnectionHandler;
        _verifyConnectionHandler = verifyConnectionHandler;
        _getConnectionHandler = getConnectionHandler;
        _logger = logger;
    }

    private bool IsAuthenticated() =>
        HttpContext.Session.GetString("Username") != null;

    [HttpGet]
    public IActionResult Index(ConnectionRole? role, ConnectionType? type, ConnectionStatus? status, string? search)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Home");

        // TODO: Implement GetConnectionsQuery handler and invoke here
        // For now, return empty view
        var viewModel = new ConnectionsIndexViewModel
        {
            Connections = Enumerable.Empty<ConnectionListItemViewModel>(),
            FilterRole = role,
            FilterType = type,
            FilterStatus = status,
            SearchTerm = search
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult New()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Home");

        var viewModel = new ConnectionFormViewModel
        {
            CurrentStep = 1,
            IsEditMode = false,
            ThrottlingProfile = ThrottlingProfile.Normal,
            PreserveTimestamps = true,
            ReplaceIllegalCharacters = true
        };

        return View("Wizard", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Home");

        var query = new GetConnectionByIdQuery { ConnectionId = id };
        var result = await _getConnectionHandler.HandleAsync(query);

        if (result.IsFailure)
        {
            if (result.Error?.Type == ErrorType.NotFound)
                return NotFound();

            TempData["ErrorMessage"] = result.Error?.Message;
            return RedirectToAction(nameof(Index));
        }

        var connection = result.Value;
        var viewModel = new ConnectionFormViewModel
        {
            Id = connection.Id,
            Name = connection.Name,
            Description = connection.Description,
            Role = connection.Role,
            Type = connection.Type,
            Status = connection.Status,
            EndpointUrl = connection.EndpointUrl,
            AuthenticationMode = connection.AuthenticationMode,
            Username = connection.Username,
            ThrottlingProfile = connection.ThrottlingProfile,
            PreserveAuthorship = connection.PreserveAuthorship,
            PreserveTimestamps = connection.PreserveTimestamps,
            ReplaceIllegalCharacters = connection.ReplaceIllegalCharacters,
            LastVerifiedAt = connection.LastVerifiedAt,
            LastVerificationResult = connection.LastVerificationResult,
            LastVerificationDiagnostics = connection.LastVerificationDiagnostics,
            CurrentStep = 1,
            IsEditMode = true
        };

        return View("Wizard", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] ConnectionFormViewModel model)
    {
        if (!IsAuthenticated())
            return Unauthorized();

        try
        {
            var createDto = new CreateConnectionDto
            {
                Name = model.Name,
                Description = model.Description,
                Role = model.Role,
                Type = model.Type,
                EndpointUrl = model.EndpointUrl,
                AuthenticationMode = model.AuthenticationMode,
                Username = model.Username,
                Password = model.Password,
                ThrottlingProfile = model.ThrottlingProfile,
                PreserveAuthorship = model.PreserveAuthorship,
                PreserveTimestamps = model.PreserveTimestamps,
                ReplaceIllegalCharacters = model.ReplaceIllegalCharacters
            };

            var command = new CreateConnectionCommand { ConnectionData = createDto };
            var result = await _createConnectionHandler.HandleAsync(command);

            if (result.IsFailure)
            {
                return BadRequest(new { success = false, message = result.Error?.Message });
            }

            return Ok(new
            {
                success = true,
                connectionId = result.Value.Id,
                message = $"Connection '{result.Value.Name}' created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving connection");
            return StatusCode(500, new { success = false, message = "An error occurred while saving the connection" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Verify(int id)
    {
        if (!IsAuthenticated())
            return Unauthorized();

        var command = new VerifyConnectionCommand { ConnectionId = id };
        var result = await _verifyConnectionHandler.HandleAsync(command);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Error?.Message ?? "Verification failed"
            });
        }

        var verification = result.Value;
        return Ok(new
        {
            success = verification.IsSuccessful,
            message = verification.Message,
            diagnostics = verification.Diagnostics,
            verifiedAt = verification.VerifiedAt
        });
    }
}
