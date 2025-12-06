using System;

namespace TuneShelf.Models;

public sealed record Playlist
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}