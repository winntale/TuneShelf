using System;
using System.Collections.Generic;

namespace TuneShelf.Models;

public sealed record Track
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required int Duration { get; init; }
    public required string Genre { get; init; }
    public required decimal Rating { get; init; }
    public required Guid AlbumId { get; init; }
    
    public string FilePath { get; init; } = string.Empty;
    
    public Album? Album { get; init; }
    public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
}