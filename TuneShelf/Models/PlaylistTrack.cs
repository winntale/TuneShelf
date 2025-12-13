using System;

namespace TuneShelf.Models;

public sealed record PlaylistTrack
{
    public required Guid PlaylistId { get; init; }
    public required Guid TrackId { get; init; }
    
    public int Order { get; set; }
    
    public Playlist? Playlist { get; set; }
    public Track?   Track    { get; set; }
}