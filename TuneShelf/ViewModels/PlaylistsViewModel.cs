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

public sealed class PlaylistsViewModel : ViewModelBase
{
    private readonly LibraryService _libraryService;
    private readonly IDialogService _dialogService;

    private readonly List<Playlist> _allPlaylists = new();
    public ObservableCollection<Playlist> Playlists { get; } = new();
    
    public sealed record PlaylistDisplay(Playlist Playlist, int TrackCount)
    {
        public Guid Id => Playlist.Id;
        public string Name => Playlist.Name;
        public string Description => Playlist.Description ?? string.Empty;
        public int TrackCount { get; } = TrackCount;
    }

    public ObservableCollection<PlaylistDisplay> PlaylistsEx { get; } = new();

    private PlaylistDisplay? _selectedPlaylistDisplay;
    public PlaylistDisplay? SelectedPlaylistDisplay
    {
        get => _selectedPlaylistDisplay;
        set
        {
            if (_selectedPlaylistDisplay == value) return;
            _selectedPlaylistDisplay = value;
            OnPropertyChanged();

            SelectedPlaylist = value?.Playlist;

            ((RelayCommand)EditPlaylistCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeletePlaylistCommand).RaiseCanExecuteChanged();
            ((RelayCommand)EnterEditModeCommand).RaiseCanExecuteChanged();

            _ = LoadTracksForSelectedAsync();
        }
    }

    private Playlist? _selectedPlaylist;
    public Playlist? SelectedPlaylist
    {
        get => _selectedPlaylist;
        set
        {
            if (_selectedPlaylist == value) return;
            _selectedPlaylist = value;
            OnPropertyChanged();

            ((RelayCommand)EditPlaylistCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeletePlaylistCommand).RaiseCanExecuteChanged();
            ((RelayCommand)EnterEditModeCommand).RaiseCanExecuteChanged();
            ((RelayCommand)AddTrackToPlaylistCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RemoveTrackFromPlaylistCommand).RaiseCanExecuteChanged();

            _ = LoadTracksForSelectedAsync();
        }
    }

    private bool _isEditing;
    private string _editedName = string.Empty;
    private string _editedDescription = string.Empty;

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (_isEditing == value) return;
            _isEditing = value;
            OnPropertyChanged();
            ((RelayCommand)EnterEditModeCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ApplyEditCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelEditCommand).RaiseCanExecuteChanged();
        }
    }

    public string EditedName
    {
        get => _editedName;
        set
        {
            if (_editedName == value) return;
            _editedName = value;
            OnPropertyChanged();
            ((RelayCommand)ApplyEditCommand).RaiseCanExecuteChanged();
        }
    }

    public string EditedDescription
    {
        get => _editedDescription;
        set
        {
            if (_editedDescription == value) return;
            _editedDescription = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Track> PlaylistTracks { get; } = new();

    private Track? _selectedTrackInLibrary;
    public Track? SelectedTrackInLibrary
    {
        get => _selectedTrackInLibrary;
        set
        {
            if (_selectedTrackInLibrary == value) return;
            _selectedTrackInLibrary = value;
            OnPropertyChanged();
            ((RelayCommand)AddTrackToPlaylistCommand).RaiseCanExecuteChanged();
        }
    }

    private Track? _selectedTrackInPlaylist;
    public Track? SelectedTrackInPlaylist
    {
        get => _selectedTrackInPlaylist;
        set
        {
            if (_selectedTrackInPlaylist == value) return;
            _selectedTrackInPlaylist = value;
            OnPropertyChanged();
            ((RelayCommand)RemoveTrackFromPlaylistCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand LoadPlaylistsCommand { get; }
    public ICommand CreatePlaylistCommand { get; }
    public ICommand EditPlaylistCommand { get; }
    public ICommand DeletePlaylistCommand { get; }
    public ICommand AddTrackToPlaylistCommand { get; }
    public ICommand RemoveTrackFromPlaylistCommand { get; }
    public ICommand EnterEditModeCommand { get; }
    public ICommand ApplyEditCommand { get; }
    public ICommand CancelEditCommand { get; }

    public PlaylistsViewModel(LibraryService libraryService, IDialogService dialogService)
    {
        _libraryService = libraryService;
        _dialogService  = dialogService;

        LoadPlaylistsCommand = new RelayCommand(async _ => await LoadPlaylistsAsync());
        CreatePlaylistCommand = new RelayCommand(async _ => await CreatePlaylistAsync());
        EditPlaylistCommand = new RelayCommand(async _ => await EditPlaylistAsync(), _ => SelectedPlaylist is not null);
        DeletePlaylistCommand = new RelayCommand(async _ => await DeletePlaylistAsync(), _ => SelectedPlaylist is not null);

        AddTrackToPlaylistCommand = new RelayCommand(
            async _ => await AddSelectedTrackAsync(),
            _ => SelectedPlaylist is not null && SelectedTrackInLibrary is not null);

        RemoveTrackFromPlaylistCommand = new RelayCommand(
            async _ => await RemoveSelectedTrackAsync(),
            _ => SelectedPlaylist is not null && SelectedTrackInPlaylist is not null);

        EnterEditModeCommand = new RelayCommand(_ => EnterEditMode(), _ => SelectedPlaylist is not null && !IsEditing);
        ApplyEditCommand = new RelayCommand(async _ => await ApplyEditAsync(), _ => IsEditing && !string.IsNullOrWhiteSpace(EditedName));
        CancelEditCommand = new RelayCommand(_ => CancelEdit(), _ => IsEditing);
    }

    public async Task LoadPlaylistsAsync()
    {
        _allPlaylists.Clear();
        Playlists.Clear();
        PlaylistsEx.Clear();

        var playlists = await _libraryService.GetAllPlaylistsAsync();
        _allPlaylists.AddRange(playlists);

        foreach (var p in playlists)
        {
            Playlists.Add(p);
            var tracks = await _libraryService.GetTracksForPlaylistAsync(p.Id);
            PlaylistsEx.Add(new PlaylistDisplay(p, tracks.Count));
        }
        
        SelectedPlaylistDisplay = null;
    }

    private async Task LoadTracksForSelectedAsync()
    {
        PlaylistTracks.Clear();
        if (SelectedPlaylist is null) return;

        var tracks = await _libraryService.GetTracksForPlaylistAsync(SelectedPlaylist.Id);
        foreach (var t in tracks)
            PlaylistTracks.Add(t);
    }

    private async Task CreatePlaylistAsync()
    {
        var edited = await _dialogService.ShowPlaylistEditorAsync(null);
        if (edited is null) return;

        var created = await _libraryService.CreatePlaylistAsync(edited);

        _allPlaylists.Add(created);
        Playlists.Add(created);

        PlaylistsEx.Add(new PlaylistDisplay(created, 0));
        SelectedPlaylist = created;
        SelectedPlaylistDisplay = PlaylistsEx.First(p => p.Id == created.Id);
    }

    private async Task EditPlaylistAsync()
    {
        if (SelectedPlaylist is null) return;

        var updated = await _dialogService.ShowPlaylistEditorAsync(SelectedPlaylist);
        if (updated is null) return;

        await _libraryService.UpdatePlaylistAsync(updated);

        var allIdx = _allPlaylists.FindIndex(p => p.Id == updated.Id);
        if (allIdx >= 0) _allPlaylists[allIdx] = updated;

        var idx = Playlists.ToList().FindIndex(p => p.Id == updated.Id);
        if (idx >= 0) Playlists[idx] = updated;

        var exIdx = PlaylistsEx.ToList().FindIndex(p => p.Id == updated.Id);
        if (exIdx >= 0)
        {
            var oldCount = PlaylistsEx[exIdx].TrackCount;
            PlaylistsEx[exIdx] = new PlaylistDisplay(updated, oldCount);
        }

        SelectedPlaylist = updated;
        SelectedPlaylistDisplay = PlaylistsEx.FirstOrDefault(p => p.Id == updated.Id);
    }

    private async Task DeletePlaylistAsync()
    {
        if (SelectedPlaylist is null) return;

        await _libraryService.DeletePlaylistAsync(SelectedPlaylist.Id);

        _allPlaylists.RemoveAll(p => p.Id == SelectedPlaylist.Id);

        var exIdx = PlaylistsEx.ToList().FindIndex(p => p.Id == SelectedPlaylist.Id);
        if (exIdx >= 0) PlaylistsEx.RemoveAt(exIdx);

        Playlists.Remove(SelectedPlaylist);
        PlaylistTracks.Clear();

        SelectedPlaylist = null;
        SelectedPlaylistDisplay = null;
    }

    private void EnterEditMode()
    {
        if (SelectedPlaylist is null) return;
        EditedName        = SelectedPlaylist.Name;
        EditedDescription = SelectedPlaylist.Description ?? string.Empty;
        IsEditing         = true;
    }

    private void CancelEdit()
    {
        IsEditing = false;
    }

    private async Task ApplyEditAsync()
    {
        if (SelectedPlaylist is null) return;

        var updated = SelectedPlaylist with
        {
            Name        = EditedName.Trim(),
            Description = string.IsNullOrWhiteSpace(EditedDescription) ? null : EditedDescription.Trim()
        };

        await _libraryService.UpdatePlaylistAsync(updated);

        var allIdx = _allPlaylists.FindIndex(p => p.Id == updated.Id);
        if (allIdx >= 0) _allPlaylists[allIdx] = updated;

        var idx = Playlists.ToList().FindIndex(p => p.Id == updated.Id);
        if (idx >= 0) Playlists[idx] = updated;

        var exIdx = PlaylistsEx.ToList().FindIndex(p => p.Id == updated.Id);
        if (exIdx >= 0)
        {
            var oldCount = PlaylistsEx[exIdx].TrackCount;
            PlaylistsEx[exIdx] = new PlaylistDisplay(updated, oldCount);
        }

        SelectedPlaylist        = updated;
        SelectedPlaylistDisplay = PlaylistsEx.FirstOrDefault(p => p.Id == updated.Id);

        IsEditing = false;
    }

    private async Task AddSelectedTrackAsync()
    {
        if (SelectedPlaylist is null || SelectedTrackInLibrary is null) return;

        await _libraryService.AddTrackToPlaylistAsync(SelectedPlaylist.Id, SelectedTrackInLibrary.Id);

        await LoadTracksForSelectedAsync();

        var exIdx = PlaylistsEx.ToList().FindIndex(p => p.Id == SelectedPlaylist.Id);
        if (exIdx >= 0)
        {
            var pd = PlaylistsEx[exIdx];
            PlaylistsEx[exIdx] = new PlaylistDisplay(pd.Playlist, pd.TrackCount + 1);
            SelectedPlaylistDisplay = PlaylistsEx[exIdx];
        }
    }

    private async Task RemoveSelectedTrackAsync()
    {
        if (SelectedPlaylist is null || SelectedTrackInPlaylist is null) return;

        await _libraryService.RemoveTrackFromPlaylistAsync(SelectedPlaylist.Id, SelectedTrackInPlaylist.Id);

        await LoadTracksForSelectedAsync();

        var exIdx = PlaylistsEx.ToList().FindIndex(p => p.Id == SelectedPlaylist.Id);
        if (exIdx >= 0)
        {
            var pd = PlaylistsEx[exIdx];
            PlaylistsEx[exIdx] = new PlaylistDisplay(pd.Playlist, Math.Max(0, pd.TrackCount - 1));
            SelectedPlaylistDisplay = PlaylistsEx[exIdx];
        }
    }
}
