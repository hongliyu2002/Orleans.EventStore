using Fluxera.Guards;

namespace Orleans.Persistence.EventStore.UnitTests.Grains;

[GenerateSerializer]
public sealed class Snack
{
    public Snack()
    {
    }

    public Snack(Guid id, string name, string? pictureUrl = null)
        : this()
    {
        Id = Guard.Against.Empty(id, nameof(id));
        Name = Guard.Against.NullOrEmpty(name, nameof(name));
        PictureUrl = pictureUrl;
    }

    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public DateTimeOffset? CreatedAt { get; set; }

    [Id(2)]
    public string? CreatedBy { get; set; }

    public bool IsCreated => CreatedAt != null;

    [Id(3)]
    public DateTimeOffset? LastModifiedAt { get; set; }

    [Id(4)]
    public string? LastModifiedBy { get; set; }

    [Id(5)]
    public DateTimeOffset? DeletedAt { get; set; }

    [Id(6)]
    public string? DeletedBy { get; set; }

    [Id(7)]
    public bool IsDeleted { get; set; }

    [Id(8)]
    public string Name { get; set; } = string.Empty;

    [Id(9)]
    public string? PictureUrl { get; set; }

    public override string ToString()
    {
        return $"Snack with Id:{Id} Name:'{Name}'";
    }
}
