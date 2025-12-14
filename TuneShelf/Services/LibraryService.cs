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
            .Include(t => t.Album)
            .ThenInclude(a => a.Artist)
            .OrderBy(t => t.Title)
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
            .Include(a => a.Tracks)
            .OrderBy(a => a.Title)
            .ToListAsync();
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


    // PLAYLISTS

    public async Task<List<Playlist>> GetAllPlaylistsAsync()
    {
        await using var db = new TuneShelfDbContext();
        return await db.Playlists
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Playlist> CreatePlaylistAsync(Playlist playlist)
    {
        await using var db = new TuneShelfDbContext();
        db.Playlists.Add(playlist);
        await db.SaveChangesAsync();
        return playlist;
    }

    public async Task UpdatePlaylistAsync(Playlist playlist)
    {
        await using var db = new TuneShelfDbContext();
        db.Playlists.Update(playlist);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeletePlaylistAsync(Guid id)
    {
        await using var db = new TuneShelfDbContext();

        var links = await db.PlaylistTracks
            .Where(pt => pt.PlaylistId == id)
            .ToListAsync();

        if (links.Count > 0)
            db.PlaylistTracks.RemoveRange(links);

        var playlist = await db.Playlists.FindAsync(id);
        if (playlist is null)
            return true;

        db.Playlists.Remove(playlist);
        await db.SaveChangesAsync();
        return true;
    }


    // PLAYLISTTRACK

    public async Task AddTrackToPlaylistAsync(Guid playlistId, Guid trackId)
    {
        await using var db = new TuneShelfDbContext();

        var exists = await db.PlaylistTracks
            .AnyAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId);
        if (exists) return;

        db.PlaylistTracks.Add(new PlaylistTrack
        {
            PlaylistId = playlistId,
            TrackId = trackId
        });

        await db.SaveChangesAsync();
    }

    public async Task RemoveTrackFromPlaylistAsync(Guid playlistId, Guid trackId)
    {
        await using var db = new TuneShelfDbContext();

        var entity = await db.PlaylistTracks
            .FirstOrDefaultAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId);
        if (entity is null) return;

        db.PlaylistTracks.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<Track>> GetTracksForPlaylistAsync(Guid playlistId)
    {
        await using var db = new TuneShelfDbContext();

        return await db.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlistId)
            .Include(pt => pt.Track)
                .ThenInclude(t => t.Album)
                .ThenInclude(a => a.Artist)
            .Select(pt => pt.Track!)
            .OrderBy(t => t.Title)
            .ToListAsync();
    }
}