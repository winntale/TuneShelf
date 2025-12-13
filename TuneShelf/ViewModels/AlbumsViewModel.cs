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

public sealed class AlbumsViewModel : ViewModelBase
{
    private readonly LibraryService _libraryService;
    private readonly IDialogService _dialogService;
    
    public ArtistsViewModel ArtistsVm { get; }
    
    public ObservableCollection<Album> Albums { get; } = new();
    private Album? _selectedAlbum;
    private readonly List<Album> _allAlbums = new();
    private string _albumSearchQuery = string.Empty;

    private Artist? _selectedArtist;
    private bool _showOnlySelectedArtistAlbums;
    
    public Album? SelectedAlbum
    {
        get => _selectedAlbum;
        set
        {
            if (_selectedAlbum == value) return;
            _selectedAlbum = value;
            OnPropertyChanged();
            ((RelayCommand)EditAlbumCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteAlbumCommand).RaiseCanExecuteChanged();
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
            ApplyFilter();
        }
    }

    public Artist? SelectedArtist
    {
        get => _selectedArtist;
        set
        {
            if (_selectedArtist == value) return;
            _selectedArtist = value;  
            OnPropertyChanged();
            ApplyFilter();
        }
    }
    
    public bool ShowOnlySelectedArtistAlbums
    {
        get => _showOnlySelectedArtistAlbums;
        set
        {
            if (_showOnlySelectedArtistAlbums == value) return;
            _showOnlySelectedArtistAlbums = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public ICommand LoadAlbumsCommand  { get; }
    public ICommand CreateAlbumCommand { get; }
    public ICommand EditAlbumCommand   { get; }
    public ICommand DeleteAlbumCommand { get; }
    

    public AlbumsViewModel(LibraryService libraryService, IDialogService dialogService, ArtistsViewModel artistsVm)
    {
        _libraryService = libraryService;
        _dialogService = dialogService;

        ArtistsVm = artistsVm;
        
        SelectedArtist = ArtistsVm.SelectedArtist;

        ArtistsVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ArtistsVm.SelectedArtist))
            {
                SelectedArtist = ArtistsVm.SelectedArtist;
            }
        };
        
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

        ApplyFilter();
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

        var ok = await _libraryService.DeleteAlbumAsync(SelectedAlbum.Id);
        if (!ok)
        {
            await _dialogService.ShowInfoAsync(
                "Удаление запрещено",
                "Нельзя удалить альбом: у него есть треки.");
            return;
        }
        
        _allAlbums.RemoveAll(a => a.Id == SelectedAlbum.Id);
        ApplyFilter();
    }
    
    private void ApplyFilter()
    {
        var previous = SelectedAlbum;

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

        if (ShowOnlySelectedArtistAlbums && SelectedArtist is not null)
        {
            var artistId = SelectedArtist.Id;
            filtered = filtered.Where(a => a.ArtistId == artistId);
        }

        foreach (var album in filtered)
            Albums.Add(album);
        
        if (previous is not null)
        {
            var match = Albums.FirstOrDefault(a => a.Id == previous.Id);
            if (match is not null)
                SelectedAlbum = match;
            else
                SelectedAlbum = null;
        }
    }


}