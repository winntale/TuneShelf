using System;
using System.Collections.Generic; // List<T>
using System.Threading.Tasks; // Task<T>
using Microsoft.EntityFrameworkCore; // AsNoTracking
using TuneShelf.Data;
using TuneShelf.Models;

namespace TuneShelf.Services;

public sealed class LibraryService
{
    public async Task<List<Track>> GetAllTracksAsync()
    {
        await using var db = new TuneShelfDbContext();
        return await db.Tracks
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddTrackAsync(Track track)
    {
        await using var db = new TuneShelfDbContext();
        db.Tracks.Add(track);
        await db.SaveChangesAsync();
    }

    public async Task UpdateTrackAsync(Track track)
    {
        await using var db = new TuneShelfDbContext();

        db.Tracks.Attach(track);
        db.Entry(track).State = EntityState.Modified;

        await db.SaveChangesAsync();
    }
    
    public async Task DeleteTrackAsync(Guid trackId)
    {
        await using var db = new TuneShelfDbContext();
        var track = await db.Tracks.FindAsync(trackId);
        if (track is null) return;

        db.Tracks.Remove(track);
        await db.SaveChangesAsync();
    }

    // DEFAULT VALUES GETTERS
    
    public async Task<Guid> GetOrCreateDefaultAlbumIdAsync()
    {
        await using var db = new TuneShelfDbContext();

        var album = await db.Albums
            .FirstOrDefaultAsync(a => a.Title == "Unknown Album");

        if (album is not null)
        {
            return album.Id;
        }
        
        var newAlbum = new Album
        {
            Id = Guid.NewGuid(),
            Title = "Unknown Album",
            Year = 0,
            ArtistId = await GetOrCreateDefaultArtistIdAsync()
        };

        db.Albums.Add(newAlbum);
        await db.SaveChangesAsync();

        return newAlbum.Id;
    }

    public async Task<Guid> GetOrCreateDefaultArtistIdAsync()
    {
        await using var db = new TuneShelfDbContext();

        var artist = await db.Artists
            .FirstOrDefaultAsync(a => a.Name == "Unknown Artist");

        if (artist is not null)
        {
            return artist.Id;
        }

        var newArtist = new Artist
        {
            Id = Guid.NewGuid(),
            Name = "Unknown Artist"
        };

        db.Artists.Add(newArtist);
        await db.SaveChangesAsync();

        return newArtist.Id;
    }
}