using Microsoft.EntityFrameworkCore;
using TuneShelf.Models;

namespace TuneShelf.Data;

public class TuneShelfDbContext : DbContext
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();
    
    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder
    )
    {
        optionsBuilder.UseSqlite("Data Source=tuneshelf.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Album>()
            .HasOne<Artist>()
            .WithMany()
            .HasForeignKey(a => a.ArtistId);

        modelBuilder.Entity<Track>()
            .HasOne<Album>()
            .WithMany()
            .HasForeignKey(t => t.AlbumId);

        modelBuilder.Entity<PlaylistTrack>()
            .HasOne<Playlist>()
            .WithMany()
            .HasForeignKey(pt => pt.PlaylistId);

        modelBuilder.Entity<PlaylistTrack>()
            .HasOne<Track>()
            .WithMany()
            .HasForeignKey(pt => pt.TrackId);
    }
}