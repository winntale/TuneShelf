using System.Collections.Generic;
using System.Threading.Tasks;
using TuneShelf.Models;

namespace TuneShelf.Interfaces;

public interface IDialogService
{
    Task<Track?> ShowTrackEditorAsync(Track? track, IReadOnlyList<Album> albums);
    Task<Album?> ShowAlbumEditorAsync(Album? album, IReadOnlyList<Artist> artists);
    Task<Artist?> ShowArtistEditorAsync(Artist? artist);
    Task ShowInfoAsync(string title, string message);
    Task<string?> PromptTextAsync(string title, string message, string? initialText = null);
    Task<Playlist?> ShowPlaylistEditorAsync(Playlist? playlist);
}