namespace ExperimentLab.Services;

public class StatsService
{
    public record TestResult(
        double ControlRate, double TreatmentRate,
        double AbsoluteLift, double RelativeLift,
        double ZScore, double PValue,
        double CiLower, double CiUpper);

    // Two-proportion z-test: is treatment's conversion rate really different from control's?
    public TestResult CompareConversion(int controlN, int controlConv,
                                        int treatmentN, int treatmentConv)
    {
        double pC = (double)controlConv / controlN;
        double pT = (double)treatmentConv / treatmentN;

        double absLift = pT - pC;
        double relLift = pC == 0 ? 0 : absLift / pC;

        // Pooled standard error -> the significance test.
        double pPool = (double)(controlConv + treatmentConv) / (controlN + treatmentN);
        double sePool = Math.Sqrt(pPool * (1 - pPool) * (1.0 / controlN + 1.0 / treatmentN));
        double z = sePool == 0 ? 0 : absLift / sePool;
        double pValue = 2 * (1 - NormalCdf(Math.Abs(z)));   // two-tailed

        // Unpooled standard error -> the confidence interval on the difference.
        double seDiff = Math.Sqrt(pC * (1 - pC) / controlN + pT * (1 - pT) / treatmentN);
        double ciLower = absLift - 1.96 * seDiff;
        double ciUpper = absLift + 1.96 * seDiff;

        return new TestResult(
            Math.Round(pC, 4), Math.Round(pT, 4),
            Math.Round(absLift, 4), Math.Round(relLift, 4),
            Math.Round(z, 4), Math.Round(pValue, 5),
            Math.Round(ciLower, 4), Math.Round(ciUpper, 4));
    }

    // Standard normal CDF — Abramowitz & Stegun 26.2.17. .NET has no built-in one.
    private static double NormalCdf(double x)
    {
        if (x < 0) return 1.0 - NormalCdf(-x);
        const double p = 0.2316419, c = 0.39894228;   // c = 1/sqrt(2*pi)
        double t = 1.0 / (1.0 + p * x);
        double poly = t * (0.319381530 + t * (-0.356563782 + t * (1.781477937
                      + t * (-1.821255978 + t * 1.330274429))));
        return 1.0 - c * Math.Exp(-x * x / 2.0) * poly;
    }
}