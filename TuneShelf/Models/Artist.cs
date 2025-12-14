using System;
using System.Collections.Generic;

namespace TuneShelf.Models;

public sealed record Artist
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    
    public ICollection<Album> Albums { get; init; } = new List<Album>();
}