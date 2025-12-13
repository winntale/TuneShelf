using System;
using System.Collections.Generic;

namespace TuneShelf.Models;

public sealed record Playlist
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "Без описания";
    
    public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
}