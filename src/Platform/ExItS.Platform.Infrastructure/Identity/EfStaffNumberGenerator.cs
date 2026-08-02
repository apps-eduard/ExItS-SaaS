using ExItS.Platform.Application.Identity;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Identity;

internal sealed class EfStaffNumberGenerator : IStaffNumberGenerator
{
    public const string Prefix = "STF-";
    private const int SequenceDigits = 6;

    private readonly PlatformDbContext _db;

    public EfStaffNumberGenerator(PlatformDbContext db) => _db = db;

    public async Task<string> GenerateNextAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _db.PlatformUsers.AsNoTracking()
            .Where(u => u.StaffNumber != null && u.StaffNumber.StartsWith(Prefix))
            .Select(u => u.StaffNumber!)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var maxSequence = 0;
        foreach (var staffNumber in existing)
        {
            if (staffNumber.Length != Prefix.Length + SequenceDigits)
            {
                continue;
            }

            if (int.TryParse(staffNumber.AsSpan(Prefix.Length), out var sequence))
            {
                maxSequence = Math.Max(maxSequence, sequence);
            }
        }

        return $"{Prefix}{(maxSequence + 1).ToString($"D{SequenceDigits}")}";
    }
}
