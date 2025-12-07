using System;
using System.Collections.ObjectModel;
using TuneShelf.Models;
using TuneShelf.Services;

namespace TuneShelf.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly LibraryService _libraryService;
    
    private string _title = "TuneShelf – Music Library";
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

    public ObservableCollection<Track> Tracks { get; init; } = new();

    public MainWindowViewModel()
    {
        _libraryService = new LibraryService();

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

        if (Tracks.Count == 0)
        {
            Title = "TuneShelf – Music Library (empty)";
        }
        else
        {
            Title = $"TuneShelf – Music Library ({Tracks.Count} tracks)";
        }
    }

    private void SeedDummyData()
    {
        Tracks.Add(new Track
        {
            Id = Guid.NewGuid(),
            Title = "Test Track 1",
            Duration = 180,
            Genre = "Rock",
            Rating = 4.5m,
            AlbumId = Guid.Empty
        });
        
        Tracks.Add(new Track
        {
            Id = Guid.NewGuid(),
            Title = "Test Track 2",
            Duration = 240,
            Genre = "Electronic",
            Rating = 5.0m,
            AlbumId = Guid.Empty
        });
    }
}