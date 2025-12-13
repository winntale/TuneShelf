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

public sealed class ArtistsViewModel : ViewModelBase
{
    private readonly LibraryService _libraryService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<Artist> Artists { get; } = new();
    public sealed record ArtistDisplay(Artist Artist, int AlbumCount)
    {
        public Guid Id => Artist.Id;
        public string Name => Artist.Name;
        public int AlbumCount { get; } = AlbumCount;
    }

    public ObservableCollection<ArtistDisplay> ArtistsEx { get; } = new();

    private ArtistDisplay? _selectedArtistDisplay;
    public ArtistDisplay? SelectedArtistDisplay
    {
        get => _selectedArtistDisplay;
        set
        {
            if (_selectedArtistDisplay == value) return;
            _selectedArtistDisplay = value;
            OnPropertyChanged();
            SelectedArtist = value is null ? null : value.Artist;
            ((RelayCommand)EditArtistCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteArtistCommand).RaiseCanExecuteChanged();
        }
    }

    private Artist? _selectedArtist;
    public Artist? SelectedArtist
    {
        get => _selectedArtist;
        set
        {
            if (_selectedArtist == value) return;
            _selectedArtist = value;
            OnPropertyChanged();
            ((RelayCommand)EditArtistCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteArtistCommand).RaiseCanExecuteChanged();
        }
    }

    private string _artistSearchQuery = string.Empty;
    public string ArtistSearchQuery
    {
        get => _artistSearchQuery;
        set
        {
            if (_artistSearchQuery == value) return;
            _artistSearchQuery = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    private readonly List<Artist> _allArtists = new();

    public ICommand LoadArtistsCommand  { get; }
    public ICommand CreateArtistCommand { get; }
    public ICommand EditArtistCommand   { get; }
    public ICommand DeleteArtistCommand { get; }
    
    public ArtistsViewModel(LibraryService libraryService, IDialogService dialogService)
    {
        _libraryService = libraryService;
        _dialogService  = dialogService;

        LoadArtistsCommand   = new RelayCommand(async _ => await LoadArtistsAsync());
        CreateArtistCommand  = new RelayCommand(async _ => await CreateArtistAsync());
        EditArtistCommand    = new RelayCommand(async _ => await EditArtistAsync(),   _ => SelectedArtist is not null);
        DeleteArtistCommand  = new RelayCommand(async _ => await DeleteArtistAsync(), _ => SelectedArtist is not null);
    }
    
    public async Task LoadArtistsAsync()
    {
        _allArtists.Clear();
        Artists.Clear();

        var artists = await _libraryService.GetAllArtistsAsync();
        var albums = await _libraryService.GetAllAlbumsAsync();
        var albumLookup = albums.GroupBy(a => a.ArtistId).ToDictionary(g => g.Key, g => g.Count());

        _allArtists.AddRange(artists);

        ArtistsEx.Clear();
        foreach (var ar in artists)
        {
            var count = albumLookup.TryGetValue(ar.Id, out var c) ? c : 0;
            ArtistsEx.Add(new ArtistDisplay(ar, count));
            Artists.Add(ar);
        }

        ApplyFilter();
    }
    
    private async Task CreateArtistAsync()
    {
        var edited = await _dialogService.ShowArtistEditorAsync(null);
        if (edited is null) return;

        var created = await _libraryService.CreateArtistAsync(edited);
        _allArtists.Add(created);
        ApplyFilter();
    }

    private async Task EditArtistAsync()
    {
        if (SelectedArtist is null) return;

        var edited = await _dialogService.ShowArtistEditorAsync(SelectedArtist);
        if (edited is null) return;

        await _libraryService.UpdateArtistAsync(edited);

        var idx = _allArtists.FindIndex(a => a.Id == edited.Id);
        if (idx >= 0) _allArtists[idx] = edited;

        ApplyFilter();
        SelectedArtist = edited;
    }

    private async Task DeleteArtistAsync()
    {
        if (SelectedArtist is null) return;

        var id = SelectedArtist.Id;
        var ok = await _libraryService.DeleteArtistAsync(id);

        if (!ok)
        {
            await _dialogService.ShowInfoAsync(
                "Удаление запрещено",
                "Нельзя удалить артиста: у него есть альбомы.");
            return;
        }
        
        _allArtists.RemoveAll(a => a.Id == id);
        ApplyFilter();
        SelectedArtist = null;
    }
    
    private void ApplyFilter()
    {
        Artists.Clear();
        ArtistsEx.Clear();

        var albums = _libraryService.GetAllAlbumsAsync().Result;
        var albumLookup = albums.GroupBy(a => a.ArtistId).ToDictionary(g => g.Key, g => g.Count());

        var query = _artistSearchQuery?.Trim();
        IEnumerable<Artist> filtered = _allArtists;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLowerInvariant();
            filtered = filtered.Where(a =>
                (!string.IsNullOrEmpty(a.Name) && a.Name.ToLowerInvariant().Contains(q)));
        }

        foreach (var artist in filtered)
        {
            Artists.Add(artist);
            ArtistsEx.Add(new ArtistDisplay(artist, albumLookup.TryGetValue(artist.Id, out var c) ? c : 0));
        }
    }

}