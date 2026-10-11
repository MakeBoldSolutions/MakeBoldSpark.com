namespace MakeBoldSpark.Api.Features.FamilyMemories;

public class FamilyMemoryService(FamilyMemoryRepository repository)
{
    public async Task<PublishedMemoriesResponse> PublishedAsync(CancellationToken ct) =>
        new(true, new(await repository.PublishedAsync(ct)));
    public Task<List<FamilyMemory>> ListAsync(CancellationToken ct) => repository.ListAsync(ct);
    public Task<FamilyMemory?> FindAsync(int id, CancellationToken ct) => repository.FindAsync(id, ct);

    public async Task<FamilyMemory> CreateAsync(FamilyMemoryWrite input, CancellationToken ct)
    {
        var memory = new FamilyMemory { CreatedDate = DateTime.UtcNow };
        Apply(memory, input);
        repository.Add(memory);
        await repository.SaveAsync(ct);
        return memory;
    }

    public async Task<FamilyMemory?> UpdateAsync(int id, FamilyMemoryWrite input, CancellationToken ct)
    {
        var memory = await repository.FindAsync(id, ct);
        if (memory is null) return null;
        Apply(memory, input);
        await repository.SaveAsync(ct);
        return memory;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var memory = await repository.FindAsync(id, ct);
        if (memory is null) return false;
        repository.Remove(memory);
        await repository.SaveAsync(ct);
        return true;
    }

    private static void Apply(FamilyMemory memory, FamilyMemoryWrite input)
    {
        memory.PersonName = input.PersonName;
        memory.PersonPronouns = input.PersonPronouns;
        memory.RelationshipToJared = input.RelationshipToJared;
        memory.Body = input.Body;
        memory.SortOrder = input.SortOrder;
        memory.IsPublished = input.IsPublished;
        memory.UpdatedDate = DateTime.UtcNow;
    }
}
