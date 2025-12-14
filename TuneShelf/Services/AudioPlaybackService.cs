using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Interfaces;
using SoundFlow.Providers;
using SoundFlow.Structs;

public sealed class AudioPlaybackService : IAsyncDisposable
{
    private readonly MiniAudioEngine _engine;
    private readonly AudioPlaybackDevice _device;
    private SoundPlayer? _player;
    private ISoundDataProvider? _provider;
    private FileStream? _currentStream;
    private bool _disposed;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AudioPlaybackService()
    {
        _engine = new MiniAudioEngine();
        var format = AudioFormat.Dvd;
        var defaultDevice = _engine.PlaybackDevices.FirstOrDefault(d => d.IsDefault);
        _device = _engine.InitializePlaybackDevice(defaultDevice, format);
        _device.Start();
    }

    public async Task PlayFileAsync(string path)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AudioPlaybackService));
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

        await _lock.WaitAsync();
        try
        {
            // Полная очистка старого состояния
            if (_player != null)
            {
                _player.Stop();
                await Task.Delay(100);
                _device.MasterMixer.RemoveComponent(_player);
                _player.Dispose();
                _player = null;
            }

            _provider?.Dispose();
            _currentStream?.Dispose();

            // Создаём новый плеер
            _currentStream = File.OpenRead(path);
            _provider = new StreamDataProvider(_engine, _device.Format, _currentStream);
            _player = new SoundPlayer(_engine, _device.Format, _provider);
            
            _device.MasterMixer.AddComponent(_player);
            await Task.Delay(50);
            _player.Play();
        }
        catch
        {
            _currentStream?.Dispose();
            _provider?.Dispose();
            _player?.Dispose();
            _currentStream = null;
            _provider = null;
            _player = null;
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    // ← ДОБАВЛЯЕМ Stop() для MainWindowViewModel
    public void Stop()
    {
        _lock.Wait();
        try
        {
            _player?.Stop();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _player?.Stop();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _lock.WaitAsync();
        try
        {
            _player?.Stop();
            if (_player != null)
            {
                _device.MasterMixer.RemoveComponent(_player);
                _player.Dispose();
            }
            
            _provider?.Dispose();
            _currentStream?.Dispose();
            
            _device.Stop();
            _device.Dispose();
            _engine.Dispose();
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }
}
