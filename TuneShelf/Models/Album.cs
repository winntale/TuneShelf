using System;
using System.Collections.Generic;

namespace TuneShelf.Models;

public sealed record Album
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required int Year { get; init; }
    public required Guid ArtistId { get; init; }
    
    public Artist? Artist { get; init; }         // <‑‑ навигация
    public ICollection<Track> Tracks { get; init; } = new List<Track>();
}