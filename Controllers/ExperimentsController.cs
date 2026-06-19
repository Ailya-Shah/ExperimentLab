using ExperimentLab.Data;
using ExperimentLab.Dtos;
using ExperimentLab.Models;
using ExperimentLab.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExperimentLab.Controllers;

[ApiController]
[Route("api/[controller]")]   // -> /api/experiments
public class ExperimentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AssignmentService _assigner;
    private readonly IConfiguration _config;

    public ExperimentsController(AppDbContext db, AssignmentService assigner, IConfiguration config)
    {
        _db = db;
        _assigner = assigner;
        _config = config;
    }

    // GET /api/experiments  -> list all experiments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExperimentDto>>> GetAll()
    {
        var experiments = await _db.Experiments
            .Include(e => e.Variants)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return Ok(experiments.Select(ToDto));
    }

    // GET /api/experiments/5  -> one experiment
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExperimentDto>> GetById(int id)
    {
        var exp = await _db.Experiments
            .Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (exp is null) return NotFound();
        return Ok(ToDto(exp));
    }

    // POST /api/experiments  -> create an experiment with its variants
    [HttpPost]
    public async Task<ActionResult<ExperimentDto>> Create(CreateExperimentDto dto)
    {
        // Business rule: variant traffic must sum to 100.
        var totalTraffic = dto.Variants.Sum(v => v.TrafficPercentage);
        if (Math.Abs(totalTraffic - 100.0) > 0.01)
            return BadRequest($"Variant traffic must sum to 100 (got {totalTraffic}).");

        // Business rule: variant names must be unique within the experiment.
        if (dto.Variants.Select(v => v.Name).Distinct().Count() != dto.Variants.Count)
            return BadRequest("Variant names must be unique within an experiment.");

        // Business rule: exactly one variant is the control (the baseline).
        if (dto.Variants.Count(v => v.IsControl) != 1)
            return BadRequest("Exactly one variant must be marked as the control (isControl: true).");

        var exp = new Experiment
        {
            Name = dto.Name,
            Description = dto.Description,
            Status = "draft",
            Variants = dto.Variants.Select(v => new Variant
            {
                Name = v.Name,
                TrafficPercentage = v.TrafficPercentage,
                IsControl = v.IsControl
            }).ToList()
        };

        _db.Experiments.Add(exp);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = exp.Id }, ToDto(exp));
    }

    // POST /api/experiments/5/start  -> move it to "running"
    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> Start(int id)
    {
        var exp = await _db.Experiments.FindAsync(id);
        if (exp is null) return NotFound();
        exp.Status = "running";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/experiments/5/stop  -> move it to "stopped"
    [HttpPost("{id:int}/stop")]
    public async Task<IActionResult> Stop(int id)
    {
        var exp = await _db.Experiments.FindAsync(id);
        if (exp is null) return NotFound();
        exp.Status = "stopped";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/experiments/5/assign?userId=alice  -> which variant this user gets
    [HttpGet("{id:int}/assign")]
    public async Task<ActionResult<object>> Assign(int id, [FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("userId is required.");

        var exp = await _db.Experiments
            .Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (exp is null) return NotFound();

        if (exp.Status != "running")
            return BadRequest($"Experiment is '{exp.Status}', not running — start it first.");

        var variant = _assigner.Assign(exp.Id, userId, exp.Variants);
        return Ok(new { experimentId = exp.Id, userId, variant = variant.Name });
    }

    // POST /api/experiments/5/simulate?users=5000
    // DEMO SEEDING ONLY — disabled by default. In production, events come from real
    // traffic, never a button. The control arm converts at baseRate; every other arm
    // converts higher by nonControlLift, so there's a real effect to detect.
    [HttpPost("{id:int}/simulate")]
    public async Task<ActionResult<object>> Simulate(int id, [FromQuery] int users = 5000)
    {
        if (!_config.GetValue<bool>("Demo:SeedingEnabled"))
            return StatusCode(StatusCodes.Status403Forbidden,
                "Demo seeding is disabled. Set Demo:SeedingEnabled = true to generate sample data.");

        var exp = await _db.Experiments
            .Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (exp is null) return NotFound();
        if (exp.Status != "running")
            return BadRequest($"Experiment is '{exp.Status}', not running — start it first.");
        if (users < 1 || users > 100000)
            return BadRequest("users must be between 1 and 100000.");

        const double baseRate = 0.10;        // control's true conversion rate
        const double nonControlLift = 0.04;  // every non-control arm converts this much higher

        var rng = new Random();
        var batch = new List<Event>(users * 2);

        for (int i = 0; i < users; i++)
        {
            var userId = $"sim_user_{i}";
            var variant = _assigner.Assign(exp.Id, userId, exp.Variants);

            // Everyone assigned sees the experience -> an exposure event.
            batch.Add(new Event { ExperimentId = exp.Id, UserId = userId,
                                  Variant = variant.Name, Type = "exposure" });

            // Roll against this variant's true rate to decide if they convert.
            double trueRate = variant.IsControl ? baseRate : baseRate + nonControlLift;

            if (rng.NextDouble() < trueRate)
                batch.Add(new Event { ExperimentId = exp.Id, UserId = userId,
                                      Variant = variant.Name, Type = "conversion" });
        }

        _db.Events.AddRange(batch);     // one bulk insert, not 10,000 round trips
        await _db.SaveChangesAsync();

        var results = batch
            .GroupBy(e => e.Variant)
            .Select(g =>
            {
                int exposures = g.Count(e => e.Type == "exposure");
                int conversions = g.Count(e => e.Type == "conversion");
                return new
                {
                    variant = g.Key,
                    exposures,
                    conversions,
                    conversionRate = exposures == 0 ? 0 : Math.Round((double)conversions / exposures, 4)
                };
            })
            .OrderBy(r => r.variant)
            .ToList();

        return Ok(new { experimentId = exp.Id, usersSimulated = users, results });
    }

    // DELETE /api/experiments/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var exp = await _db.Experiments.FindAsync(id);
        if (exp is null) return NotFound();
        _db.Experiments.Remove(exp);   // variants cascade-delete
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- mapping helper: entity -> DTO ----
    private static ExperimentDto ToDto(Experiment e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        Status = e.Status,
        CreatedAt = e.CreatedAt,
        Variants = e.Variants.Select(v => new VariantDto
        {
            Id = v.Id,
            Name = v.Name,
            TrafficPercentage = v.TrafficPercentage,
            IsControl = v.IsControl
        }).ToList()
    };
}