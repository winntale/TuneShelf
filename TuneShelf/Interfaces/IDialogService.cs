using System.Threading.Tasks;
using TuneShelf.Models;

namespace TuneShelf.Interfaces;

public interface IDialogService
{
    Task<Album?> ShowAlbumEditorAsync(Album? album);
    Task<Artist?> ShowArtistEditorAsync(Artist? artist);
}