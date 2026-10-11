using MakeBoldSpark.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MakeBoldSpark.Api.Features.FamilyMemories;

public class FamilyMemoryRepository(MakeBoldSparkDbContext db)
{
    public Task<List<PublishedMemory>> PublishedAsync(CancellationToken ct) => db.FamilyMemories
        .AsNoTracking().Where(m => m.IsPublished).OrderBy(m => m.SortOrder).ThenBy(m => m.Id)
        .Select(m => new PublishedMemory(m.PersonName, m.PersonPronouns, m.RelationshipToJared, m.Body))
        .ToListAsync(ct);

    public Task<List<FamilyMemory>> ListAsync(CancellationToken ct) => db.FamilyMemories
        .AsNoTracking().OrderBy(m => m.SortOrder).ThenBy(m => m.Id).ToListAsync(ct);

    public Task<FamilyMemory?> FindAsync(int id, CancellationToken ct) =>
        db.FamilyMemories.SingleOrDefaultAsync(m => m.Id == id, ct);

    public void Add(FamilyMemory memory) => db.FamilyMemories.Add(memory);
    public void Remove(FamilyMemory memory) => db.FamilyMemories.Remove(memory);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
