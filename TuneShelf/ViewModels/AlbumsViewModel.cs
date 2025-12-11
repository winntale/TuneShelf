using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TuneShelf.Commands;
using TuneShelf.Interfaces;
using TuneShelf.Models;
using TuneShelf.Services;

namespace TuneShelf.ViewModels;

public class AlbumsViewModel : ViewModelBase
{
    private readonly LibraryService _libraryService;

    public ObservableCollection<Album> Albums { get; } = new();
    private Album? _selectedAlbum;
    private readonly List<Album> _allAlbums = new();
    private string _albumSearchQuery = string.Empty;
    
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
    
    public string AlbumSearchQuery
    {
        get => _albumSearchQuery;
        set
        {
            if (_albumSearchQuery == value) return;
            _albumSearchQuery = value;
            OnPropertyChanged();
            ApplyAlbumFilter();
        }
    }

    public ICommand LoadAlbumsCommand  { get; }
    public ICommand CreateAlbumCommand { get; }
    public ICommand EditAlbumCommand   { get; }
    public ICommand DeleteAlbumCommand { get; }

    private readonly IDialogService _dialogService;

    public AlbumsViewModel(LibraryService libraryService, IDialogService dialogService)
    {
        _libraryService = libraryService;
        _dialogService = dialogService;
        LoadAlbumsCommand  = new RelayCommand(async _ => await LoadAsync());
        CreateAlbumCommand = new RelayCommand(async _ => await CreateAsync());
        EditAlbumCommand   = new RelayCommand(async _ => await EditAsync(),   _ => SelectedAlbum is not null);
        DeleteAlbumCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedAlbum is not null);
    }

    public async Task LoadAsync()
    {
        _allAlbums.Clear();
        Albums.Clear();

        var albums = await _libraryService.GetAllAlbumsAsync();
        _allAlbums.AddRange(albums);

        ApplyAlbumFilter();
    }

    private async Task CreateAsync()
    {
        var edited = await _dialogService.ShowAlbumEditorAsync(null);
        if (edited is null) return;

        var created = await _libraryService.CreateAlbumAsync(edited);
        Albums.Add(created);
    }

    private async Task EditAsync()
    {
        if (SelectedAlbum is null) return;

        var edited = await _dialogService.ShowAlbumEditorAsync(SelectedAlbum);
        if (edited is null) return;

        await _libraryService.UpdateAlbumAsync(edited);

        var idx = Albums.IndexOf(SelectedAlbum);
        Albums[idx] = edited;
        SelectedAlbum = edited;
    }

    private async Task DeleteAsync()
    {
        if (SelectedAlbum is null) return;

        await _libraryService.DeleteAlbumAsync(SelectedAlbum.Id);
        Albums.Remove(SelectedAlbum);
    }
    
    private void ApplyAlbumFilter()
    {
        Albums.Clear();

        var query = _albumSearchQuery?.Trim();
        IEnumerable<Album> filtered = _allAlbums;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLowerInvariant();

            filtered = filtered.Where(a =>
                (!string.IsNullOrEmpty(a.Title) && a.Title.ToLowerInvariant().Contains(q)) ||
                a.Year.ToString().Contains(q));
        }

        foreach (var album in filtered)
            Albums.Add(album);
    }

}