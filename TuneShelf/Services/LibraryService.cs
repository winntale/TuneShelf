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
}