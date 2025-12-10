using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TuneShelf.Commands;
using TuneShelf.Interfaces;
using TuneShelf.Models;
using TuneShelf.Services;
using TuneShelf.ViewModels;

public class AlbumsViewModel : ViewModelBase
{
    private readonly LibraryService _library;

    public ObservableCollection<Album> Albums { get; } = new();
    private Album? _selectedAlbum;
    public Album? SelectedAlbum
    {
        get => _selectedAlbum;
        set
        {
            if (_selectedAlbum == value) return;
            _selectedAlbum = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadAlbumsCommand  { get; }
    public ICommand CreateAlbumCommand { get; }
    public ICommand EditAlbumCommand   { get; }
    public ICommand DeleteAlbumCommand { get; }

    private readonly IDialogService _dialogService;

    public AlbumsViewModel(LibraryService library, IDialogService dialogService)
    {
        _library = library;
        _dialogService = dialogService;
        LoadAlbumsCommand  = new RelayCommand(async _ => await LoadAsync());
        CreateAlbumCommand = new RelayCommand(async _ => await CreateAsync());
        EditAlbumCommand   = new RelayCommand(async _ => await EditAsync(),   _ => SelectedAlbum is not null);
        DeleteAlbumCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedAlbum is not null);
    }

    public async Task LoadAsync()
    {
        Albums.Clear();
        var albums = await _library.GetAllAlbumsAsync();
        foreach (var a in albums)
            Albums.Add(a);
    }

    private async Task CreateAsync()
    {
        var edited = await _dialogService.ShowAlbumEditorAsync(null);
        if (edited is null) return;

        var created = await _library.CreateAlbumAsync(edited);
        Albums.Add(created);
    }

    private async Task EditAsync()
    {
        if (SelectedAlbum is null) return;

        var edited = await _dialogService.ShowAlbumEditorAsync(SelectedAlbum);
        if (edited is null) return;

        await _library.UpdateAlbumAsync(edited);

        var idx = Albums.IndexOf(SelectedAlbum);
        Albums[idx] = edited;
        SelectedAlbum = edited;
    }

    private async Task DeleteAsync()
    {
        if (SelectedAlbum is null) return;

        await _library.DeleteAlbumAsync(SelectedAlbum.Id);
        Albums.Remove(SelectedAlbum);
    }
}