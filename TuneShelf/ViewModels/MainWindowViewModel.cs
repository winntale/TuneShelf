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
    private readonly IDialogService _dialogService;
    private readonly AudioPlaybackService _audioService;

    public AlbumsViewModel AlbumsVm { get; }
    public ArtistsViewModel ArtistsVm { get; }
    public PlaylistsViewModel PlaylistsVm { get; }
    public MiniPlayerViewModel MiniPlayer { get; }

    private string _title = "TuneShelf – Музыкальная библиотека";
    private int _newTrackDurationSeconds = 0;
    private decimal _newTrackRating = 0m;

    private Track? _selectedTrack = null;
    private string _editTrackTitle = string.Empty;
    private string _editTrackGenre = string.Empty;
    private int _editTrackDurationSeconds;
    private decimal _editTrackRating;

    private readonly List<Track> _allTracks = new();
    private string _searchQuery = string.Empty;

    private int _selectedTabIndex;

    private decimal? _minRating;
    private decimal? _maxRating;
    private string? _selectedGenreFilter;

    private Album? _selectedAlbum;
    private bool _showOnlySelectedAlbumTracks;

    private int _currentTrackIndex = -1;


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

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex == value) return;
            _selectedTabIndex = value;
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
            ((RelayCommand)PlayPlaylistTrackCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PlaySelectedTrackCommand).RaiseCanExecuteChanged();
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

    public int CurrentTrackIndex
    {
        get => _currentTrackIndex;
        private set
        {
            if (_currentTrackIndex == value) return;
            _currentTrackIndex = value;
            OnPropertyChanged();
            ((RelayCommand)PlayNextTrackCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PlayPreviousTrackCommand).RaiseCanExecuteChanged();
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

    public ICommand NavigateToTracksCommand { get; }
    public ICommand NavigateToAlbumsCommand { get; }
    public ICommand NavigateToArtistsCommand { get; }
    public ICommand NavigateToPlaylistsCommand { get; }

    public ICommand RefreshAllCommand { get; }

    public ICommand PlaySelectedTrackCommand { get; }
    public ICommand PlayNextTrackCommand { get; }
    public ICommand PlayPreviousTrackCommand { get; }
    public ICommand PausePlaybackCommand { get; }
    public ICommand StopPlaybackCommand { get; }
    public ICommand PlayPlaylistTrackCommand { get; }

    public MainWindowViewModel()
    {
        _libraryService = new LibraryService();
        _dialogService = new DialogService();
        _audioService = new AudioPlaybackService();
        ArtistsVm = new ArtistsViewModel(_libraryService, _dialogService);
        AlbumsVm = new AlbumsViewModel(_libraryService, _dialogService, ArtistsVm);
        MiniPlayer = new MiniPlayerViewModel(_audioService);
        PlaylistsVm = new PlaylistsViewModel(_libraryService, _dialogService, MiniPlayer);

        _audioService.StateChanged += (_, _) => OnAudioStateChanged();

        AlbumsVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AlbumsVm.SelectedAlbum))
            {
                SelectedAlbum = AlbumsVm.SelectedAlbum;
            }
        };

        AddTrackCommand = new RelayCommand(async _ => await AddTrackAsync());

        UpdateTrackCommand = new RelayCommand(
            async _ => await UpdateSelectedTrackAsync(),
            _ => SelectedTrack is not null && !string.IsNullOrWhiteSpace(EditTrackTitle));

        DeleteTrackCommand = new RelayCommand(
            async _ => await DeleteSelectedTrackAsync(),
            _ => SelectedTrack is not null);
        NavigateToTracksCommand = new RelayCommand(_ => SelectedTabIndex = 0);
        NavigateToAlbumsCommand = new RelayCommand(_ => SelectedTabIndex = 1);
        NavigateToArtistsCommand = new RelayCommand(_ => SelectedTabIndex = 2);
        NavigateToPlaylistsCommand = new RelayCommand(_ => SelectedTabIndex = 3);
        RefreshAllCommand = new RelayCommand(async _ =>
        {
            LoadTracks();
            await AlbumsVm.LoadAsync();
            await ArtistsVm.LoadArtistsAsync();
            await PlaylistsVm.LoadPlaylistsAsync();
        });

        PlaySelectedTrackCommand = new RelayCommand(
            async _ => await PlaySelectedTrackAsync(),
            _ => SelectedTrack is not null &&
                 !string.IsNullOrWhiteSpace(SelectedTrack.FilePath));

        PlayNextTrackCommand = new RelayCommand(async _ => await PlayNextAsync(), _ => CanMoveNext());
        PlayPreviousTrackCommand = new RelayCommand(async _ => await PlayPreviousAsync(), _ => CanMovePrevious());

        PausePlaybackCommand = new RelayCommand(_ => _audioService.Pause());

        StopPlaybackCommand = new RelayCommand(
            _ => MiniPlayer.OnStoppedFromOutside(),
            _ => _audioService.State is PlaybackState.Playing or PlaybackState.Paused);

        PlayPlaylistTrackCommand = new RelayCommand(
            async _ => await PlayPlaylistTrackAsync(),
            _ => PlaylistsVm.SelectedTrackInPlaylist is not null &&
                 !string.IsNullOrWhiteSpace(PlaylistsVm.SelectedTrackInPlaylist.FilePath));

        PlaylistsVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlaylistsViewModel.SelectedTrackInPlaylist))
                ((RelayCommand)PlayPlaylistTrackCommand).RaiseCanExecuteChanged();
        };


        LoadTracks();
        _ = AlbumsVm.LoadAsync();
        _ = ArtistsVm.LoadArtistsAsync();
        _ = PlaylistsVm.LoadPlaylistsAsync();
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
            ? "TuneShelf – Музыкальная библиотека (пусто)"
            : $"TuneShelf – Музыкальная библиотека ({Tracks.Count} треков)";
    }

    private async Task AddTrackAsync()
    {
        var albums = await _libraryService.GetAllAlbumsAsync();
        if (albums.Count == 0)
            return;

        var track = await _dialogService.ShowTrackEditorAsync(null, albums);
        if (track is null)
            return;

        await _libraryService.AddTrackAsync(track);

        _allTracks.Add(track);
        LoadTracks();
    }

    private async Task UpdateSelectedTrackAsync()
    {
        if (SelectedTrack is null)
            return;

        var albums = await _libraryService.GetAllAlbumsAsync();

        var edited = await _dialogService.ShowTrackEditorAsync(SelectedTrack, albums);
        if (edited is null)
            return;

        await _libraryService.UpdateTrackAsync(edited);

        var index = Tracks.IndexOf(SelectedTrack);
        if (index >= 0)
        {
            Tracks[index] = edited;
            SelectedTrack = edited;
        }

        var allIndex = _allTracks.FindIndex(t => t.Id == edited.Id);
        if (allIndex >= 0)
            _allTracks[allIndex] = edited;

        LoadTracks();
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

    private async Task PlaySelectedTrackAsync()
    {
        if (SelectedTrack is null || string.IsNullOrWhiteSpace(SelectedTrack.FilePath))
            return;

        await MiniPlayer.StartFromAsync(Tracks.ToList(), SelectedTrack, playlist: null);
    }

    public async Task PlayPlaylistTrackAsync()
    {
        if (PlaylistsVm.SelectedTrackInPlaylist is null) return;
        var track = PlaylistsVm.SelectedTrackInPlaylist;
        if (string.IsNullOrWhiteSpace(track.FilePath)) return;

        await _audioService.PlayAsync(track.FilePath);
    }

    private bool CanMoveNext()
    {
        return Tracks.Count > 0 && CurrentTrackIndex >= 0 && CurrentTrackIndex < Tracks.Count - 1;
    }

    private bool CanMovePrevious()
    {
        return Tracks.Count > 0 && CurrentTrackIndex > 0;
    }

    private async Task PlayNextAsync()
    {
        if (!CanMoveNext()) return;

        CurrentTrackIndex++;
        var next = Tracks[CurrentTrackIndex];
        if (string.IsNullOrWhiteSpace(next.FilePath)) return;

        await _audioService.SwitchTrackAsync(next.FilePath);
        SelectedTrack = next;
    }

    private async Task PlayPreviousAsync()
    {
        if (!CanMovePrevious()) return;

        CurrentTrackIndex--;
        var prev = Tracks[CurrentTrackIndex];
        if (string.IsNullOrWhiteSpace(prev.FilePath)) return;

        await _audioService.SwitchTrackAsync(prev.FilePath);
        SelectedTrack = prev;
    }

    private void OnAudioStateChanged()
    {
        ((RelayCommand)StopPlaybackCommand).RaiseCanExecuteChanged();
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
}