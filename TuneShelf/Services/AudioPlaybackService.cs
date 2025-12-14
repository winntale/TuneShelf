using System;
using System.Threading.Tasks;
using NAudio.Wave;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}

public sealed class AudioPlaybackService : IAsyncDisposable
{
    private IWavePlayer? _output;
    private AudioFileReader? _reader;

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public string? CurrentFilePath { get; private set; }

    public event EventHandler? StateChanged;

    private void SetState(PlaybackState newState)
    {
        if (State == newState) return;
        State = newState;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task PlayAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (State == PlaybackState.Paused &&
            string.Equals(path, CurrentFilePath, StringComparison.OrdinalIgnoreCase))
        {
            _output?.Play();
            SetState(PlaybackState.Playing);
            return;
        }

        Cleanup();

        CurrentFilePath = path;
        _reader = new AudioFileReader(path);
        _output = new WaveOutEvent();
        _output.Init(_reader);
        _output.Play();
        SetState(PlaybackState.Playing);

        await Task.CompletedTask;
    }

    public void Pause()
    {
        if (State != PlaybackState.Playing || _output is null)
            return;

        _output.Pause();
        SetState(PlaybackState.Paused);
    }

    public void Stop()
    {
        if (_output is null && State == PlaybackState.Stopped)
            return;

        _output?.Stop();
        SetState(PlaybackState.Stopped);
    }

    public async Task SwitchTrackAsync(string newPath)
    {
        Stop();
        Cleanup();
        await PlayAsync(newPath);
    }

    private void Cleanup()
    {
        _output?.Stop();
        _output?.Dispose();
        _reader?.Dispose();

        _output = null;
        _reader = null;
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        Cleanup();
        await Task.CompletedTask;
    }
}
