using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using TuneShelf.Commands;
using TuneShelf.Models;

namespace TuneShelf.ViewModels;

public sealed class MiniPlayerViewModel : ViewModelBase
{
    private readonly AudioPlaybackService _audio;

    private IReadOnlyList<Track> _currentList = Array.Empty<Track>();
    private int _currentIndex = -1;

    public IReadOnlyList<Track> CurrentList
    {
        get => _currentList;
        private set
        {
            _currentList = value ?? Array.Empty<Track>();
            OnPropertyChanged();
            UpdateCanExecute();
        }
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        private set
        {
            if (_currentIndex == value) return;
            _currentIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentTrack));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(ArtistName));
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(Line1));
            OnPropertyChanged(nameof(Line2));
            UpdateCanExecute();
        }
    }
    
    private Playlist? _currentPlaylist;
    public Playlist? CurrentPlaylist
    {
        get => _currentPlaylist;
        set
        {
            if (_currentPlaylist == value) return;
            _currentPlaylist = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Line2));
        }
    }

    public string Line1 =>
        CurrentTrack is null
            ? "No track"
            : (string.IsNullOrWhiteSpace(ArtistName)
                ? Title
                : $"{Title} – {ArtistName}");

    public string Line2
    {
        get
        {
            if (CurrentTrack is null)
                return string.Empty;

            return CurrentPlaylist is null
                ? "Library"
                : CurrentPlaylist.Name;
        }
    }

    public Track? CurrentTrack =>
        CurrentIndex >= 0 && CurrentIndex < CurrentList.Count
            ? CurrentList[CurrentIndex]
            : null;

    public string Title      => CurrentTrack?.Title  ?? "No track";
    public string ArtistName => CurrentTrack?.Album?.Artist?.Name ?? string.Empty;
    public bool IsVisible    => CurrentTrack is not null;

    public ICommand PlayPauseCommand  { get; }
    public ICommand StopCommand       { get; }
    public ICommand NextCommand       { get; }
    public ICommand PreviousCommand   { get; }

    public MiniPlayerViewModel(AudioPlaybackService audio)
    {
        _audio = audio;

        PlayPauseCommand = new RelayCommand(_ => PlayPause(), _ => CurrentTrack is not null);
        StopCommand = new RelayCommand(_ => _audio.Stop(), _ => CurrentTrack is not null);
        NextCommand = new RelayCommand(async _ => await PlayNextAsync(),     _ => CanMoveNext());
        PreviousCommand = new RelayCommand(async _ => await PlayPreviousAsync(), _ => CanMovePrevious());
    }

    private void UpdateCanExecute()
    {
        ((RelayCommand)PlayPauseCommand).RaiseCanExecuteChanged();
        ((RelayCommand)StopCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PreviousCommand).RaiseCanExecuteChanged();
    }

    private bool CanMoveNext() =>
        CurrentList.Count > 0 && CurrentIndex >= 0 && CurrentIndex < CurrentList.Count - 1;

    private bool CanMovePrevious() =>
        CurrentList.Count > 0 && CurrentIndex > 0;

    private async Task PlayNextAsync()
    {
        if (!CanMoveNext()) return;
        CurrentIndex++;
        await StartCurrentAsync();
    }

    private async Task PlayPreviousAsync()
    {
        if (!CanMovePrevious()) return;
        CurrentIndex--;
        await StartCurrentAsync();
    }

    private async Task StartCurrentAsync()
    {
        if (CurrentTrack is null || string.IsNullOrWhiteSpace(CurrentTrack.FilePath))
            return;

        await _audio.SwitchTrackAsync(CurrentTrack.FilePath);
    }

    private void PlayPause()
    {
        if (CurrentTrack is null || string.IsNullOrWhiteSpace(CurrentTrack.FilePath))
            return;

        switch (_audio.State)
        {
            case PlaybackState.Stopped:
                _ = _audio.PlayAsync(CurrentTrack.FilePath);
                break;
            case PlaybackState.Playing:
                _audio.Pause();
                break;
            case PlaybackState.Paused:
                _ = _audio.PlayAsync(CurrentTrack.FilePath);
                break;
        }
    }

    // публичный метод для VM: задать список и трек
    public async Task StartFromAsync(IReadOnlyList<Track> list, Track track, Playlist? playlist = null)
    {
        CurrentList     = list;
        CurrentPlaylist = playlist;
        CurrentIndex    = list.IndexOf(track);
        if (CurrentIndex < 0)
            CurrentIndex = 0;

        OnPropertyChanged(nameof(Line1));
        OnPropertyChanged(nameof(Line2));

        await StartCurrentAsync();
    }
}
