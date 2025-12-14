using System;
using System.Diagnostics;
using System.IO;
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

    private readonly string _logPath = Path.Combine(Path.GetTempPath(), "tuneshelf_seek.log");

    private void Log(string message)
    {
        try
        {
            File.AppendAllText(_logPath, $"{DateTime.Now:O} {message}\n");
        }
        catch
        {
            // ignore
        }
    }

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

        Log($"[PlayAsync] Requested file={path}");

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

    public double DurationSeconds => _reader?.TotalTime.TotalSeconds ?? 0.0;

    public double PositionSeconds => _reader?.CurrentTime.TotalSeconds ?? 0.0;

    public async Task SeekAsync(double seconds)
    {
        if (_reader is null)
            return;

        try
        {
            var clamped = Math.Clamp(seconds, 0, _reader.TotalTime.TotalSeconds);

            var wasPlaying = State == PlaybackState.Playing;

            // Pause output to avoid clicks while changing position
            if (_output is not null && wasPlaying)
                _output.Pause();

            _reader.CurrentTime = TimeSpan.FromSeconds(clamped);

            Debug.WriteLine($"[Seek] Requested={seconds}, Clamped={clamped}, File={CurrentFilePath}, WasPlaying={wasPlaying}");
            Debug.WriteLine($"[Seek] After set CurrentTime={_reader.CurrentTime.TotalSeconds}");
            Log($"[Seek] Requested={seconds}, Clamped={clamped}, File={CurrentFilePath}, WasPlaying={wasPlaying}, After={_reader.CurrentTime.TotalSeconds}");

            if (_output is not null && wasPlaying)
                _output.Play();
        }
        catch (Exception)
        {
            // Swallow exceptions for now; seeking may not be supported for some sources.
        }

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

        Log("[Stop] called");
        _output?.Stop();
        SetState(PlaybackState.Stopped);
    }

    public async Task SwitchTrackAsync(string newPath)
    {
        Log($"[SwitchTrackAsync] newPath={newPath}");
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
