using ExperimentLab.Data;
using ExperimentLab.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExperimentLab.Controllers;

[ApiController]
[Route("api/experiments/{id:int}/results")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly StatsService _stats;
    private readonly DecisionService _decision;

    public ResultsController(AppDbContext db, StatsService stats, DecisionService decision)
    {
        _db = db;
        _stats = stats;
        _decision = decision;
    }

    // GET /api/experiments/1/results
    [HttpGet]
    public async Task<ActionResult<object>> Get(int id)
    {
        var exp = await _db.Experiments.Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (exp is null) return NotFound();

        var control = exp.Variants.FirstOrDefault(v => v.IsControl);
        if (control is null)
            return BadRequest("This experiment has no control variant defined.");

        // Aggregate counts in the database (GROUP BY) — never load every event into memory.
        var tallies = await _db.Events
            .Where(e => e.ExperimentId == id)
            .GroupBy(e => new { e.Variant, e.Type })
            .Select(g => new { g.Key.Variant, g.Key.Type, Count = g.Count() })
            .ToListAsync();

        int Exposures(string v)   => tallies.Where(t => t.Variant == v && t.Type == "exposure").Sum(t => t.Count);
        int Conversions(string v) => tallies.Where(t => t.Variant == v && t.Type == "conversion").Sum(t => t.Count);

        int cN = Exposures(control.Name), cConv = Conversions(control.Name);
        if (cN == 0)
            return Ok(new { message = "Not enough data yet — the control has no exposures." });

        double cRate = Math.Round((double)cConv / cN, 4);

        // Compare the control against each other arm (any number, any names).
        var comparisons = new List<object>();
        foreach (var v in exp.Variants.Where(v => !v.IsControl).OrderBy(v => v.Id))
        {
            int tN = Exposures(v.Name), tConv = Conversions(v.Name);
            if (tN == 0)
            {
                comparisons.Add(new { variant = v.Name, message = "No data yet for this arm." });
                continue;
            }

            var r = _stats.CompareConversion(cN, cConv, tN, tConv);
            var d = _decision.Decide(r, cN, tN);

            comparisons.Add(new
            {
                variant = v.Name,
                exposures = tN,
                conversions = tConv,
                rate = r.TreatmentRate,
                absoluteLift = r.AbsoluteLift,
                relativeLift = r.RelativeLift,
                zScore = r.ZScore,
                pValue = r.PValue,
                confidenceInterval95 = new { lower = r.CiLower, upper = r.CiUpper },
                significant = r.PValue < 0.05,
                decision = new { verdict = d.Verdict, reason = d.Reason }
            });
        }

        return Ok(new
        {
            experimentId = id,
            experimentName = exp.Name,
            control = new { name = control.Name, exposures = cN, conversions = cConv, rate = cRate },
            comparisons
        });
    }
}