namespace ExperimentLab.Services;

public class DecisionService
{
    public record Decision(string Verdict, string Reason);

    // minLift: smallest absolute lift worth shipping for (0.01 = 1 percentage point).
    // minSamplePerArm: refuse to decide until each arm has this many exposures.
    public Decision Decide(StatsService.TestResult r, int controlN, int treatmentN,
                           double minLift = 0.01, int minSamplePerArm = 1000)
    {
        // Gate 1: enough data? (guards against deciding on noise / peeking too early)
        if (controlN < minSamplePerArm || treatmentN < minSamplePerArm)
            return new("KEEP_RUNNING",
                $"Not enough data yet — need {minSamplePerArm}+ per variant.");

        // Gate 2: is the difference statistically real?
        if (r.PValue >= 0.05)
            return new("NO_DIFFERENCE",
                "No statistically significant difference; keep the control.");

        // Gate 3: is it big enough to be worth acting on?
        if (Math.Abs(r.AbsoluteLift) < minLift)
            return new("HOLD",
                $"Significant, but the {r.AbsoluteLift:P1} lift is below the {minLift:P0} worth shipping for.");

        string winner = r.AbsoluteLift > 0 ? "treatment" : "control";
        string p = r.PValue < 0.001 ? "< 0.001" : $"= {r.PValue:F4}";
        return new("SHIP",
            $"Ship {winner} — {Math.Abs(r.RelativeLift):P0} relative lift, p {p}.");
    }
}