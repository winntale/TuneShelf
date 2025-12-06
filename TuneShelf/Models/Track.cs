using System;

namespace TuneShelf.Models;

public sealed record Track
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required int Duration { get; init; }
    public required string Genre { get; init; }
    public required decimal Rating { get; init; }
    public required Guid AlbumId { get; init; }
}