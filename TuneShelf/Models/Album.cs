using System;

namespace TuneShelf.Models;

public sealed record Album
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required int Year { get; init; }
    public required Guid ArtistId { get; init; }
}