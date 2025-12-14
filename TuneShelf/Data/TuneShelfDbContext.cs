using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using TuneShelf.Models;

namespace TuneShelf.Data;

public class TuneShelfDbContext : DbContext
{
    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder
    )
    {
        var dbPath = Path.Combine(
            AppContext.BaseDirectory,
            "tuneshelf.db");
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
    
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Artist>()
            .HasMany(a => a.Albums)
            .WithOne(al => al.Artist)
            .HasForeignKey(al => al.ArtistId);

        modelBuilder.Entity<Album>()
            .HasMany(al => al.Tracks)
            .WithOne(t => t.Album)
            .HasForeignKey(t => t.AlbumId);

        modelBuilder.Entity<PlaylistTrack>()
            .HasKey(pt => new { pt.PlaylistId, pt.TrackId });

        modelBuilder.Entity<PlaylistTrack>()
            .HasOne(pt => pt.Playlist)
            .WithMany(p => p.PlaylistTracks)
            .HasForeignKey(pt => pt.PlaylistId);

        modelBuilder.Entity<PlaylistTrack>()
            .HasOne(pt => pt.Track)
            .WithMany(t => t.PlaylistTracks)
            .HasForeignKey(pt => pt.TrackId);
    }

}