using System.Security.Cryptography;
using System.Text;
using ExperimentLab.Models;

namespace ExperimentLab.Services;

// Decides which variant a user falls into — deterministically.
// Same user + same experiment ALWAYS returns the same variant.
// That stability is what makes it a valid experiment, not random noise.
public class AssignmentService
{
    public Variant Assign(int experimentId, string userId, IList<Variant> variants)
    {
        // Never trust the order the database hands variants back in.
        var ordered = variants.OrderBy(v => v.Id).ToList();

        double bucket = BucketFor(experimentId, userId);   // a number in [0, 100)

        double cumulative = 0;
        foreach (var v in ordered)
        {
            cumulative += v.TrafficPercentage;
            if (bucket < cumulative) return v;
        }
        return ordered[^1];   // floating-point safety net
    }

    private static double BucketFor(int experimentId, string userId)
    {
        // Mixing in experimentId so a user isn't "control" in every test.
        var key = $"{experimentId}:{userId}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        uint value = BitConverter.ToUInt32(hash, 0);
        return (value % 10000) / 100.0;   // 0.00 .. 99.99
    }
}