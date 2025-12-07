using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TuneShelf.Commands;
using TuneShelf.Models;
using TuneShelf.Services;

namespace TuneShelf.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly LibraryService _libraryService;
    
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

    public ObservableCollection<Track> Tracks { get; init; } = new();

    public ICommand AddTrackCommand { get; }
    public ICommand UpdateTrackCommand { get; }
    public ICommand DeleteTrackCommand { get; }
    
    public MainWindowViewModel()
    {
        _libraryService = new LibraryService();

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
    }

    private void LoadTracks()
    {
        Tracks.Clear();

        var tracks = _libraryService.GetAllTracksAsync().Result;

        foreach (var track in tracks)
        {
            Tracks.Add(track);
        }

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

        Tracks.Add(track);
        UpdateTitle();
        
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

        Tracks.Remove(SelectedTrack);
        UpdateTitle();
        
        SelectedTrack = null;
    }
}