using System;
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

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly LibraryService _libraryService;

    public AlbumsViewModel AlbumsVm { get; }
    public ArtistsViewModel ArtistsVm { get; }
    
    private string _title = "TuneShelf – Music Library";
    private string _newTrackTitle = string.Empty;
    private string _newTrackGenre = string.Empty;
    private int _newTrackDurationSeconds = 0;
    private decimal _newTrackRating = 0m;
    
    private Track? _selectedTrack = null;
    private string _editTrackTitle = string.Empty;
    private string _editTrackGenre = string.Empty;
    private int _editTrackDurationSeconds;
    private decimal _editTrackRating;
    
    private readonly List<Track> _allTracks = new();
    private string _searchQuery = string.Empty;
    
    private decimal? _minRating;
    private decimal? _maxRating;
    private string? _selectedGenreFilter;
    
    private Album? _selectedAlbum;
    private bool _showOnlySelectedAlbumTracks;
    
    public string Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
            OnPropertyChanged();
        }
    }

    public string NewTrackTitle
    {
        get => _newTrackTitle;
        set
        {
            if (_newTrackTitle == value) return;
            _newTrackTitle = value;
            OnPropertyChanged();
            ((RelayCommand)AddTrackCommand).RaiseCanExecuteChanged();
        }
    }

    public string NewTrackGenre
    {
        get => _newTrackGenre;
        set
        {
            if (_newTrackGenre == value) return;
            _newTrackGenre = value;
            OnPropertyChanged();
        }
    }

    public int NewTrackDurationSeconds
    {
        get => _newTrackDurationSeconds;
        set
        {
            if (_newTrackDurationSeconds == value) return;
            _newTrackDurationSeconds = value;
            OnPropertyChanged();
        }
    }

    public decimal NewTrackRating
    {
        get => _newTrackRating;
        set
        {
            if (_newTrackRating == value) return;
            _newTrackRating = value;
            OnPropertyChanged();
        }
    }

    public Track? SelectedTrack
    {
        get => _selectedTrack;
        set
        {
            if (_selectedTrack == value) return;
            _selectedTrack = value;
            OnPropertyChanged();

            if (_selectedTrack is not null)
            {
                EditTrackTitle = _selectedTrack.Title;
                EditTrackGenre = _selectedTrack.Genre;
                EditTrackDurationSeconds = _selectedTrack.Duration;
                EditTrackRating = _selectedTrack.Rating;
            }
            
            ((RelayCommand)UpdateTrackCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteTrackCommand).RaiseCanExecuteChanged();
        }
    }
    
    public string EditTrackTitle
    {
        get => _editTrackTitle;
        set
        {
            if (_editTrackTitle == value) return;
            _editTrackTitle = value;
            OnPropertyChanged();
            ((RelayCommand)UpdateTrackCommand).RaiseCanExecuteChanged();
        }
    }

    public string EditTrackGenre
    {
        get => _editTrackGenre;
        set
        {
            if (_editTrackGenre == value) return;
            _editTrackGenre = value;
            OnPropertyChanged();
        }
    }

    public int EditTrackDurationSeconds
    {
        get => _editTrackDurationSeconds;
        set
        {
            if (_editTrackDurationSeconds == value) return;
            _editTrackDurationSeconds = value;
            OnPropertyChanged();
        }
    }

    public decimal EditTrackRating
    {
        get => _editTrackRating;
        set
        {
            if (_editTrackRating == value) return;
            _editTrackRating = value;
            OnPropertyChanged();
        }
    }
    
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value) return;
            _searchQuery = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }
    
    public decimal? MinRating
    {
        get => _minRating;
        set
        {
            if (_minRating == value) return;
            _minRating = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public decimal? MaxRating
    {
        get => _maxRating;
        set
        {
            if (_maxRating == value) return;
            _maxRating = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }
    
    public string? SelectedGenreFilter
    {
        get => _selectedGenreFilter;
        set
        {
            if (_selectedGenreFilter == value) return;
            _selectedGenreFilter = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public Album? SelectedAlbum
    {
        get => _selectedAlbum;
        set
        {
            if (_selectedAlbum == value) return;
            _selectedAlbum = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }
    
    public bool ShowOnlySelectedAlbumTracks
    {
        get => _showOnlySelectedAlbumTracks;
        set
        {
            if (_showOnlySelectedAlbumTracks == value) return;
            _showOnlySelectedAlbumTracks = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }
    
    public IReadOnlyList<string> AvailableGenres =>
        _allTracks.Select(t => t.Genre)
            .Distinct()
            .OrderBy(g => g)
            .Prepend("All")
            .ToList();

    public ObservableCollection<Track> Tracks { get; init; } = new();

    public ICommand AddTrackCommand { get; }
    public ICommand UpdateTrackCommand { get; }
    public ICommand DeleteTrackCommand { get; }
    
    public MainWindowViewModel()
    {
        _libraryService = new LibraryService();
        var dialogService = new DialogService();
        AlbumsVm = new AlbumsViewModel(_libraryService, dialogService);
        ArtistsVm = new ArtistsViewModel(_libraryService, dialogService);
        
        SelectedAlbum = AlbumsVm.SelectedAlbum;
        
        AlbumsVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AlbumsVm.SelectedAlbum))
            {
                SelectedAlbum = AlbumsVm.SelectedAlbum;
            }
        };

        AddTrackCommand = new RelayCommand(
            async _ => await AddTrackAsync(),
            _ => !string.IsNullOrWhiteSpace(NewTrackTitle));

        UpdateTrackCommand = new RelayCommand(
            async _ => await UpdateSelectedTrackAsync(),
            _ => SelectedTrack is not null && !string.IsNullOrWhiteSpace(EditTrackTitle));
        
        DeleteTrackCommand = new RelayCommand(
            async _ => await DeleteSelectedTrackAsync(),
            _ => SelectedTrack is not null);
        
        LoadTracks();
        _ = AlbumsVm.LoadAsync();
    }

    private void LoadTracks()
    {
        _allTracks.Clear();
        Tracks.Clear();

        var tracks = _libraryService.GetAllTracksAsync().Result;
        _allTracks.AddRange(tracks);

        ApplyFilter();

        UpdateTitle();
    }

    private void UpdateTitle()
    {
        Title = Tracks.Count == 0
            ? "TuneShelf – Music Library (empty)"
            : $"TuneShelf – Music Library ({Tracks.Count} tracks)";
    }

    private async Task AddTrackAsync()
    {
        var track = new Track
        {
            Id = Guid.NewGuid(),
            Title = NewTrackTitle,
            Genre = string.IsNullOrWhiteSpace(NewTrackGenre) ? "Unknown" : NewTrackGenre,
            Duration = NewTrackDurationSeconds > 0 ? NewTrackDurationSeconds : 180,
            Rating = NewTrackRating,
            AlbumId = await _libraryService.GetOrCreateDefaultAlbumIdAsync()
        };

        await _libraryService.AddTrackAsync(track);
        
        _allTracks.Add(track);
        ApplyFilter();
        
        NewTrackTitle = string.Empty;
        NewTrackGenre = string.Empty;
        NewTrackDurationSeconds = 0;
        NewTrackRating = 0m;
    }

    private async Task UpdateSelectedTrackAsync()
    {
        if (SelectedTrack is null) return;

        var updated = SelectedTrack with
        {
            Title = EditTrackTitle,
            Genre = string.IsNullOrWhiteSpace(EditTrackGenre) ? "Unknown" : EditTrackGenre,
            Duration = EditTrackDurationSeconds > 0 ? EditTrackDurationSeconds : 180,
            Rating = EditTrackRating
        };

        await _libraryService.UpdateTrackAsync(updated);

        var index = Tracks.IndexOf(SelectedTrack);
        if (index >= 0)
        {
            Tracks[index] = updated;
            SelectedTrack = updated;
        }
    }
    
    private async Task DeleteSelectedTrackAsync()
    {
        if (SelectedTrack is null) return;

        var id = SelectedTrack.Id;

        await _libraryService.DeleteTrackAsync(id);
        
        _allTracks.RemoveAll(t => t.Id == id);
        ApplyFilter();
        
        SelectedTrack = null;
    }
    
    private void ApplyFilter()
    {
        Tracks.Clear();

        var query = _searchQuery?.Trim();
        IEnumerable<Track> filtered = _allTracks;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLowerInvariant();
            filtered = filtered.Where(t =>
                t.Title.ToLowerInvariant().Contains(q) ||
                t.Genre.ToLowerInvariant().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(SelectedGenreFilter) &&
            SelectedGenreFilter != "All")
        {
            filtered = filtered.Where(t => t.Genre == SelectedGenreFilter);
        }

        if (MinRating is not null)
        {
            filtered = filtered.Where(t => t.Rating >= MinRating.Value);
        }

        if (MaxRating is not null)
        {
            filtered = filtered.Where(t => t.Rating <= MaxRating.Value);
        }

        if (ShowOnlySelectedAlbumTracks && SelectedAlbum is not null)
        {
            filtered = filtered.Where(t => t.AlbumId == SelectedAlbum.Id);
        }
        
        foreach (var track in filtered)
            Tracks.Add(track);

        UpdateTitle();
    }

    
    // albums
    public async Task<List<Album>> GetAllAlbumsAsync()
        => await _libraryService.GetAllAlbumsAsync();
    
    
    // интеграция диалогового окна
    public async Task CreateTrackFromDialogAsync(Track track)
    {
        await _libraryService.AddTrackAsync(track);

        _allTracks.Add(track);
        ApplyFilter();
    }

    public async Task UpdateTrackFromDialogAsync(Track updated)
    {
        await _libraryService.UpdateTrackAsync(updated);

        var index = Tracks.IndexOf(SelectedTrack!);
        if (index >= 0)
        {
            Tracks[index] = updated;
            SelectedTrack = updated;
        }

        var allIndex = _allTracks.FindIndex(t => t.Id == updated.Id);
        if (allIndex >= 0)
        {
            _allTracks[allIndex] = updated;
        }

        ApplyFilter();
    }

}