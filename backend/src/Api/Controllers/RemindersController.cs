using Joby.Application.DTOs.Applications;
using Joby.Application.DTOs.Reminders;
using Joby.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Joby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;

    public RemindersController(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    [HttpPost]
    public async Task<ActionResult<ReminderDto>> CreateReminder([FromBody] CreateReminderRequest request)
    {
        var userId = GetUserId();
        var reminder = await _reminderService.CreateReminderAsync(userId, request);
        return Ok(reminder);
    }

    [HttpGet]
    public async Task<ActionResult<List<ReminderDto>>> GetReminders([FromQuery] bool includePast = false)
    {
        var userId = GetUserId();
        var reminders = await _reminderService.GetRemindersAsync(userId, includePast);
        return Ok(reminders);
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<List<ReminderDto>>> GetUpcomingReminders([FromQuery] int days = 7)
    {
        var userId = GetUserId();
        var reminders = await _reminderService.GetUpcomingRemindersAsync(userId, days);
        return Ok(reminders);
    }

    [HttpPut("{reminderId}/snooze")]
    public async Task<ActionResult<ReminderDto>> SnoozeReminder(
        Guid reminderId,
        [FromBody] SnoozeReminderRequest request)
    {
        var userId = GetUserId();
        var reminder = await _reminderService.SnoozeReminderAsync(userId, reminderId, request);
        return Ok(reminder);
    }

    [HttpPut("{reminderId}/complete")]
    public async Task<ActionResult<ReminderDto>> CompleteReminder(Guid reminderId)
    {
        var userId = GetUserId();
        var reminder = await _reminderService.CompleteReminderAsync(userId, reminderId);
        return Ok(reminder);
    }

    [HttpPut("{reminderId}/dismiss")]
    public async Task<ActionResult<ReminderDto>> DismissReminder(Guid reminderId)
    {
        var userId = GetUserId();
        var reminder = await _reminderService.DismissReminderAsync(userId, reminderId);
        return Ok(reminder);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}




