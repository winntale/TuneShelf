using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using DynamicData;
using TuneShelf.Commands;
using TuneShelf.Models;

namespace TuneShelf.ViewModels;

public sealed class MiniPlayerViewModel : ViewModelBase
{
    private readonly AudioPlaybackService _audio;
    
    private readonly DispatcherTimer _progressTimer;

    private IReadOnlyList<Track> _currentList = [];
    private int _currentIndex = -1;

    public IReadOnlyList<Track> CurrentList
    {
        get => _currentList;
        private set
        {
            _currentList = value;
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
            ? "Трек не выбран"
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
                ? "Библиотека"
                : CurrentPlaylist.Name;
        }
    }
    
    private double _positionSeconds;
    public double PositionSeconds
    {
        get => _positionSeconds;
        private set
        {
            if (Math.Abs(_positionSeconds - value) < 0.001) return;
            _positionSeconds = value;
            OnPropertyChanged();
        }
    }

    private double _sliderPosition;
    public double SliderPosition
    {
        get => _sliderPosition;
        set
        {
            if (Math.Abs(_sliderPosition - value) < 0.001) return;
            _sliderPosition = value;
            try
            {
                var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tuneshelf_seek.log");
                System.IO.File.AppendAllText(logPath,
                    $"{DateTime.Now:O} [SliderSetter] NewValue={value}, Duration={DurationSeconds}, IsUserSeeking={_isUserSeeking}\nStack:\n{Environment.StackTrace}\n");
            }
            catch { }
            OnPropertyChanged();
        }
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        private set
        {
            if (Math.Abs(_durationSeconds - value) < 0.001) return;
            _durationSeconds = value;
            OnPropertyChanged();
        }
    }
    private double _durationSeconds;

    private bool _isUserSeeking;

    public Track? CurrentTrack =>
        CurrentIndex >= 0 && CurrentIndex < CurrentList.Count
            ? CurrentList[CurrentIndex]
            : null;

    public string Title => CurrentTrack?.Title ?? "Трек не выбран";
    public string ArtistName => CurrentTrack?.Album?.Artist?.Name ?? string.Empty;
    public bool IsVisible => CurrentTrack is not null;

    public ICommand PlayPauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }

    public MiniPlayerViewModel(AudioPlaybackService audio)
    {
        _audio = audio;
        _audio.StateChanged += (_, _) => UpdateCanExecute();


        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _progressTimer.Tick += (_, _) => UpdatePosition();
        
        PlayPauseCommand = new RelayCommand(_ => PlayPause(), _ => CurrentTrack is not null);
        StopCommand = new RelayCommand(_ => OnStoppedFromOutside(), 
            _ => _audio.State == PlaybackState.Playing || _audio.State == PlaybackState.Paused);
        NextCommand = new RelayCommand(async _ => await PlayNextAsync(), _ => CanMoveNext());
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

        PositionSeconds = 0;
        await _audio.SwitchTrackAsync(CurrentTrack.FilePath);
        DurationSeconds = _audio.DurationSeconds;
        PositionSeconds = _audio.PositionSeconds;
        SliderPosition = PositionSeconds;
        _progressTimer.Start();
    }

    private void PlayPause()
    {
        if (CurrentTrack is null || string.IsNullOrWhiteSpace(CurrentTrack.FilePath))
            return;

        switch (_audio.State)
        {
            case PlaybackState.Stopped:
                PositionSeconds = 0;
                _ = _audio.PlayAsync(CurrentTrack.FilePath);
                _progressTimer.Start();
                break;
            case PlaybackState.Playing:
                _audio.Pause();
                _progressTimer.Stop();
                break;
            case PlaybackState.Paused:
                _ = _audio.PlayAsync(CurrentTrack.FilePath);
                _progressTimer.Start();
                break;
        }
    }
    
    public void OnStoppedFromOutside()
    {
        _audio.Stop();
        _progressTimer.Stop();
        PositionSeconds = 0;
    }

    public async Task StartFromAsync(IReadOnlyList<Track> list, Track track, Playlist? playlist = null)
    {
        CurrentList = list;
        CurrentPlaylist = playlist;
        CurrentIndex = list.IndexOf(track);
        if (CurrentIndex < 0)
            CurrentIndex = 0;

        DurationSeconds = CurrentTrack?.Duration ?? 0;
        PositionSeconds = 0;
        SliderPosition  = 0;
        
        OnPropertyChanged(nameof(Line1));
        OnPropertyChanged(nameof(Line2));

        await StartCurrentAsync();
    }
    
    public async Task SeekAsync(double newPositionSeconds)
    {
        if (CurrentTrack is null || string.IsNullOrWhiteSpace(CurrentTrack.FilePath))
            return;

        newPositionSeconds = Math.Clamp(newPositionSeconds, 0, DurationSeconds);

        PositionSeconds = newPositionSeconds;
        SliderPosition  = newPositionSeconds;
        await _audio.SeekAsync(newPositionSeconds);
        PositionSeconds = _audio.PositionSeconds;
        SliderPosition = PositionSeconds;
    }
    
    public void BeginUserSeek()
    {
        _isUserSeeking = true;
        _progressTimer.Stop();
    }

    public async Task EndUserSeekAsync(double sliderValue)
    {
        _isUserSeeking = false;
        System.Diagnostics.Debug.WriteLine($"[MiniPlayer] EndUserSeekAsync: sliderValue={sliderValue}");
        try
        {
            System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tuneshelf_seek.log"),
                $"{DateTime.Now:O} [MiniPlayer] EndUserSeekAsync: sliderValue={sliderValue}\n");
        }
        catch { }

        // perform seek and keep in 'seeking' state briefly to avoid UpdatePosition race
        _isUserSeeking = true;
        await SeekAsync(sliderValue);

        System.Diagnostics.Debug.WriteLine($"[MiniPlayer] After SeekAsync: PositionSeconds={PositionSeconds}, SliderPosition={SliderPosition}");
        try
        {
            System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tuneshelf_seek.log"),
                $"{DateTime.Now:O} [MiniPlayer] After SeekAsync: PositionSeconds={PositionSeconds}, SliderPosition={SliderPosition}\n");
        }
        catch { }

        // wait briefly to let audio backend stabilize, then resume updates
        await Task.Delay(300);
        _isUserSeeking = false;
        _progressTimer.Start();
    }

    
    private void UpdatePosition()
    {
        if (_audio.State != PlaybackState.Playing || CurrentTrack is null)
            return;

        PositionSeconds = _audio.PositionSeconds;
        try
        {
            System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tuneshelf_seek.log"),
                $"{DateTime.Now:O} [UpdatePosition] AudioPos={_audio.PositionSeconds}, VMPos={PositionSeconds}, Slider={SliderPosition}\n");
        }
        catch { }
        if (PositionSeconds > DurationSeconds)
            PositionSeconds = DurationSeconds;

        if (!_isUserSeeking)
            SliderPosition = PositionSeconds;
    }
}