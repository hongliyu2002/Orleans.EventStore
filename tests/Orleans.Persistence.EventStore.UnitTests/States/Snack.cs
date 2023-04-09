﻿namespace Vending.Domain.Abstractions.Snacks;

/// <summary>
///     Represents a snack.
/// </summary>
[Serializable]
[GenerateSerializer]
public sealed class Snack
{
    /// <summary>
    ///     The unique identifier of the snack.
    /// </summary>
    [Id(0)]
    public Guid Id { get; private set; }

    /// <summary>
    ///     The name of the snack.
    /// </summary>
    [Id(1)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    ///     The URL of the picture of the snack.
    /// </summary>
    [Id(2)]
    public string? PictureUrl { get; private set; }

    /// <summary>
    ///     The date and time when the snack was created.
    /// </summary>
    [Id(3)]
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <summary>
    ///     The user who created the snack.
    /// </summary>
    [Id(4)]
    public string? CreatedBy { get; private set; }

    /// <summary>
    ///     Indicates whether the snack has been created.
    /// </summary>
    public bool IsCreated => CreatedAt.HasValue;

    /// <summary>
    ///     The date and time when the snack was last modified.
    /// </summary>
    [Id(5)]
    public DateTimeOffset? LastModifiedAt { get; private set; }

    /// <summary>
    ///     The user who last modified the snack.
    /// </summary>
    [Id(6)]
    public string? LastModifiedBy { get; private set; }

    /// <summary>
    ///     The date and time when the snack was deleted.
    /// </summary>
    [Id(7)]
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    ///     The user who deleted the snack.
    /// </summary>
    [Id(8)]
    public string? DeletedBy { get; private set; }

    /// <summary>
    ///     Indicates whether the snack has been deleted.
    /// </summary>
    [Id(9)]
    public bool IsDeleted { get; private set; }

    public override string ToString()
    {
        return $"Snack with Id:'{Id}' Name:'{Name}' PictureUrl:'{PictureUrl}'";
    }

    #region Apply

    public void Apply(SnackInitializeCommand command)
    {
        Id = command.SnackId;
        Name = command.Name;
        PictureUrl = command.PictureUrl;
        CreatedAt = command.OperatedAt;
        CreatedBy = command.OperatedBy;
    }

    public void Apply(SnackDeleteCommand command)
    {
        DeletedAt = command.OperatedAt;
        DeletedBy = command.OperatedBy;
        IsDeleted = true;
    }

    public void Apply(SnackUpdateCommand command)
    {
        Name = command.Name;
        PictureUrl = command.PictureUrl;
        LastModifiedAt = command.OperatedAt;
        LastModifiedBy = command.OperatedBy;
    }

    #endregion

}
