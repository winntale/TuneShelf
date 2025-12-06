using System;

namespace TuneShelf.Models;

public sealed record PlaylistTrack
{
    public required Guid Id { get; init; }
    public required Guid PlaylistId { get; init; }
    public required Guid TrackId { get; init; }
}