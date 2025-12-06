using System;

namespace TuneShelf.Models;

public sealed record Artist
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}