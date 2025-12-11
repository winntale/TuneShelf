using System;
using System.Collections.ObjectModel; // ObservableCollection<T>
using System.Windows.Input;
using TuneShelf.Commands;
using TuneShelf.Models;
using TuneShelf.Services;

namespace TuneShelf.ViewModels;

public sealed class TrackEditorViewModel : ViewModelBase
{
    private readonly LibraryService _libraryService;
    
    private string _dialogTitle = "Track editor";
    private string _title = string.Empty;
    private string _genre = string.Empty;
    private int _durationSeconds;
    private decimal _rating;
    private Album? _selectedAlbum;

    public event EventHandler? CloseRequested;

    public string DialogTitle
    {
        get => _dialogTitle;
        set
        {
            if (_dialogTitle == value) return;
            _dialogTitle = value;
            OnPropertyChanged();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(IsSaveEnabled));
        }
    }

    public string Genre
    {
        get => _genre;
        set
        {
            if (_genre == value) return;
            _genre = value;
            OnPropertyChanged();
        }
    }

    public int DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (_durationSeconds == value) return;
            _durationSeconds = value;
            OnPropertyChanged();
        }
    }

    public decimal Rating
    {
        get => _rating;
        set
        {
            if (_rating == value) return;
            _rating = value;
            OnPropertyChanged();
        }
    }
    
    public Album? SelectedAlbum
    {
        get => _selectedAlbum;
        set
        {
            // Всегда обновляем значение и уведомляем об изменении для правильной работы привязки ComboBox
            _selectedAlbum = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(IsSaveEnabled));
        }
    }
    
    public bool IsConfirmed { get; private set; }
    public Track? ResultTrack { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    
    public bool IsSaveEnabled => SaveCommand?.CanExecute(null) ?? false;

    private readonly Track? _originalTrack;
    
    public ObservableCollection<Album> Albums { get; } = new();
    
    public TrackEditorViewModel(Track? track = null)
    {
        _originalTrack = track;

        if (track is not null)
        {
            DialogTitle = "Edit track";
            Title = track.Title;
            Genre = track.Genre;
            DurationSeconds = track.Duration;
            Rating = track.Rating;
         // SelectedAlbum
        }
        else
        {
            DialogTitle = "New track";
        }

        SaveCommand = new RelayCommand(_ => OnSave(), _ => 
            !string.IsNullOrWhiteSpace(Title));

        CancelCommand = new RelayCommand(_ => OnCancel());
    }


    private void OnSave()
    {
        if (SelectedAlbum is null)
        {
            IsConfirmed = false;
            ResultTrack = null;
            return;
        }

        IsConfirmed = true;

        ResultTrack = _originalTrack is null
            ? new Track
            {
                Id = Guid.NewGuid(),
                Title = Title,
                Genre = string.IsNullOrWhiteSpace(Genre) ? "Unknown" : Genre,
                Duration = DurationSeconds > 0 ? DurationSeconds : 180,
                Rating = Rating,
                AlbumId = SelectedAlbum.Id
            }
            : _originalTrack with
            {
                Title = Title,
                Genre = string.IsNullOrWhiteSpace(Genre) ? "Unknown" : Genre,
                Duration = DurationSeconds > 0 ? DurationSeconds : 180,
                Rating = Rating,
                AlbumId = SelectedAlbum.Id
            };
        
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancel()
    {
        IsConfirmed = false;
        ResultTrack = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
