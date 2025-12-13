using System;
using System.Collections.Generic;
using System.Linq; // List<T>
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
    
    
    // ALBUMS
    
    public async Task<List<Album>> GetAllAlbumsAsync()
    {
        await using var db = new TuneShelfDbContext();
        return await db.Albums
            .AsNoTracking()
            .OrderBy(a => a.Title)
            .ToListAsync();
    }
    

    public async Task<Album?> GetAlbumByIdAsync(Guid id)
    {
        await using var db = new TuneShelfDbContext();
        return await db.Albums.FindAsync(id);
    }

    public async Task<Album> CreateAlbumAsync(Album album)
    {
        await using var db = new TuneShelfDbContext();
        db.Albums.Add(album);
        await db.SaveChangesAsync();
        return album;
    }

    public async Task UpdateAlbumAsync(Album album)
    {
        await using var db = new TuneShelfDbContext();
        db.Albums.Update(album);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAlbumAsync(Guid id)
    {
        await using var db = new TuneShelfDbContext();
        
        var hasTracks = await db.Tracks.AnyAsync(t => t.AlbumId == id);
        if (hasTracks)
            return false;
        
        var album = await db.Albums.FindAsync(id);
        if (album is null)
            return true;

        db.Albums.Remove(album);
        await db.SaveChangesAsync();
        return true;
    }
    
    
    // ARTISTS
    
    public async Task<List<Artist>> GetAllArtistsAsync()
    {
        await using var db = new TuneShelfDbContext();
        return await db.Artists
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Artist> CreateArtistAsync(Artist artist)
    {
        await using var db = new TuneShelfDbContext();
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        return artist;
    }

    public async Task UpdateArtistAsync(Artist artist)
    {
        await using var db = new TuneShelfDbContext();
        db.Artists.Update(artist);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteArtistAsync(Guid id)
    {
        await using var db = new TuneShelfDbContext();
        
        var hasAlbums = await db.Albums.AnyAsync(a => a.ArtistId == id);
        if (hasAlbums)
            return false;
        
        var artist = await db.Artists.FindAsync(id);
        if (artist is null) 
            return true;

        db.Artists.Remove(artist);
        await db.SaveChangesAsync();
        return true;
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