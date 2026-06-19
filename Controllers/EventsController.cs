using ExperimentLab.Data;
using ExperimentLab.Dtos;
using ExperimentLab.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExperimentLab.Controllers;

[ApiController]
[Route("api/[controller]")]   // -> /api/events
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;
    public EventsController(AppDbContext db) => _db = db;

    // POST /api/events  -> record one event
    [HttpPost]
    public async Task<IActionResult> Record(RecordEventDto dto)
    {
        var ev = new Event
        {
            ExperimentId = dto.ExperimentId,
            UserId = dto.UserId,
            Variant = dto.Variant,
            Type = dto.Type,
            Value = dto.Value
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return Ok(new { ev.Id, ev.ExperimentId, ev.UserId, ev.Variant, ev.Type, ev.Timestamp });
    }

    // GET /api/events/1  -> recent events for an experiment
    [HttpGet("{experimentId:int}")]
    public async Task<ActionResult<object>> ForExperiment(int experimentId)
    {
        var events = await _db.Events
            .Where(e => e.ExperimentId == experimentId)
            .OrderByDescending(e => e.Timestamp)
            .Take(200)
            .ToListAsync();
        return Ok(events);
    }
}