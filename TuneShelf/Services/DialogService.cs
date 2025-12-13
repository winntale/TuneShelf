using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using TuneShelf.Interfaces;
using TuneShelf.Models;

namespace TuneShelf.Services;

public sealed class DialogService : IDialogService
{
    public async Task<Track?> ShowTrackEditorAsync(Track? track, IReadOnlyList<Album> albums)
    {
        var window = new Window
        {
            Title = track is null ? "Создать трек" : "Редактировать трек",
            Width = 450,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // title
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // genre
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // album
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // duration + rating
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // buttons

        var titleBox = new TextBox
        {
            Watermark = "Название трека",
            Text      = track?.Title ?? string.Empty,
            Margin    = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(titleBox, 0);

        var genreBox = new TextBox
        {
            Watermark = "Жанр",
            Text      = track?.Genre ?? string.Empty,
            Margin    = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(genreBox, 1);

        var albumCombo = new ComboBox
        {
            ItemsSource = albums,
            SelectedItem = albums.FirstOrDefault(a => a.Id == track?.AlbumId) 
                           ?? albums.FirstOrDefault(),
            Margin = new Thickness(0, 0, 0, 10)
        };
        albumCombo.ItemTemplate = new FuncDataTemplate<Album>(
            _ => true,
            a => new TextBlock { Text = a.Title },
            true);
        Grid.SetRow(albumCombo, 2);

        var bottomPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(bottomPanel, 3);

        var durationBox = new TextBox
        {
            Watermark = "Длительность (сек)",
            Text      = track?.Duration.ToString() ?? "180",
            Width     = 120
        };
        var ratingBox = new TextBox
        {
            Watermark = "Рейтинг",
            Text      = track?.Rating.ToString() ?? "0",
            Width     = 80
        };
        bottomPanel.Children.Add(durationBox);
        bottomPanel.Children.Add(ratingBox);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };
        Grid.SetRow(buttons, 4);

        var okButton = new Button { Content = "OK", Width = 80 };
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        grid.Children.Add(titleBox);
        grid.Children.Add(genreBox);
        grid.Children.Add(albumCombo);
        grid.Children.Add(bottomPanel);
        grid.Children.Add(buttons);

        window.Content = grid;

        var tcs = new TaskCompletionSource<Track?>();

        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(titleBox.Text))
                return;
            if (albumCombo.SelectedItem is not Album selectedAlbum)
                return;

            if (!int.TryParse(durationBox.Text, out var duration) || duration <= 0)
                duration = 180;
            if (!decimal.TryParse(ratingBox.Text, out var rating))
                rating = 0m;

            var result = track is null
                ? new Track
                {
                    Id       = Guid.NewGuid(),
                    Title    = titleBox.Text,
                    Genre    = string.IsNullOrWhiteSpace(genreBox.Text) ? "Unknown" : genreBox.Text,
                    Duration = duration,
                    Rating   = rating,
                    AlbumId  = selectedAlbum.Id
                }
                : track with
                {
                    Title    = titleBox.Text,
                    Genre    = string.IsNullOrWhiteSpace(genreBox.Text) ? "Unknown" : genreBox.Text,
                    Duration = duration,
                    Rating   = rating,
                    AlbumId  = selectedAlbum.Id
                };

            if (!tcs.Task.IsCompleted)
                tcs.SetResult(result);

            window.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);

            window.Close();
        };

        window.Closing += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);
        };

        Window? parent = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            parent = desktop.MainWindow;

        if (parent is null)
            window.Show();
        else
            await window.ShowDialog(parent);

        return await tcs.Task;
    }

    
    public async Task<Album?> ShowAlbumEditorAsync(Album? album, IReadOnlyList<Artist> artists)
    {
        var window = new Window
        {
            Title = album is null ? "Создать альбом" : "Редактировать альбом",
            Width = 400,
            Height = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // title
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // year
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // artist
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // buttons

        var titleBox = new TextBox
        {
            Watermark = "Название альбома",
            Text      = album?.Title ?? string.Empty,
            Margin    = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(titleBox, 0);

        var yearBox = new NumericUpDown
        {
            Watermark = "Год",
            Value     = album?.Year ?? DateTime.Now.Year,
            Minimum   = 1900,
            Maximum   = DateTime.Now.Year + 1,
            Margin    = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(yearBox, 1);

        var artistCombo = new ComboBox
        {
            ItemsSource   = artists,
            Margin        = new Thickness(0, 0, 0, 10)
        };
        artistCombo.ItemTemplate = new FuncDataTemplate<Artist>(
            _ => true,
            a => new TextBlock { Text = a.Name },
            true);

        artistCombo.SelectedItem =
            artists.FirstOrDefault(a => a.Id == album?.ArtistId) ??
            artists.FirstOrDefault();

        Grid.SetRow(artistCombo, 2);

        var buttons = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing             = 10
        };
        var okButton     = new Button { Content = "OK",     Width = 80 };
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        Grid.SetRow(buttons, 3);

        grid.Children.Add(titleBox);
        grid.Children.Add(yearBox);
        grid.Children.Add(artistCombo);
        grid.Children.Add(buttons);
        window.Content = grid;

        var tcs = new TaskCompletionSource<Album?>();

        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(titleBox.Text))
                return;
            if (artistCombo.SelectedItem is not Artist selectedArtist)
                return;

            var result = album is null
                ? new Album
                {
                    Id       = Guid.NewGuid(),
                    Title    = titleBox.Text,
                    Year     = (int)(yearBox.Value ?? DateTime.Now.Year),
                    ArtistId = selectedArtist.Id
                }
                : album with
                {
                    Title    = titleBox.Text,
                    Year     = (int)(yearBox.Value ?? DateTime.Now.Year),
                    ArtistId = selectedArtist.Id
                };

            if (!tcs.Task.IsCompleted)
                tcs.SetResult(result);

            window.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);
            window.Close();
        };

        window.Closing += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);
        };

        Window? parent = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            parent = desktop.MainWindow;

        await window.ShowDialog(parent);
        return await tcs.Task;
    }

    
    public async Task<Artist?> ShowArtistEditorAsync(Artist? artist) 
    {
        var window = new Window
        {
            Title = artist is null ? "Создать артиста" : "Редактировать артиста",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var nameBox = new TextBox
        {
            Watermark = "Имя артиста",
            Text      = artist?.Name ?? string.Empty,
            Margin    = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(nameBox, 0);

        var buttons = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing             = 10
        };
        Grid.SetRow(buttons, 1);

        var okButton     = new Button { Content = "OK",     Width = 80 };
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        grid.Children.Add(nameBox);
        grid.Children.Add(buttons);
        window.Content = grid;

        var tcs = new TaskCompletionSource<Artist?>();

        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
                return;

            var result = new Artist
            {
                Id   = artist?.Id ?? Guid.NewGuid(),
                Name = nameBox.Text
            };

            if (!tcs.Task.IsCompleted)
                tcs.SetResult(result);

            window.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);

            window.Close();
        };

        window.Closing += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);
        };

        Window? parent = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            parent = desktop.MainWindow;

        if (parent is null)
            window.Show();
        else
            await window.ShowDialog(parent);

        return await tcs.Task;
    }

    
    // INFO WINDOW
    
    public async Task ShowInfoAsync(string title, string message)
    {
        var window = new Window
        {
            Title = title,
            Width = 350,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stack = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10
        };

        stack.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });

        var button = new Button { Content = "OK", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
        stack.Children.Add(button);

        window.Content = stack;

        button.Click += (_, _) => window.Close();

        Window? parent = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            parent = desktop.MainWindow;

        if (parent is null)
            window.Show();
        else
            await window.ShowDialog(parent);
    }
    
    
    // PROMPT TEXT
    
    public async Task<string?> PromptTextAsync(string title, string message, string? initialText = null)
    {
        var window = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stack = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10
        };

        stack.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        });

        var textBox = new TextBox
        {
            Text = initialText ?? string.Empty
        };
        stack.Children.Add(textBox);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        var okButton = new Button { Content = "OK", Width = 80 };
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        stack.Children.Add(buttons);
        window.Content = stack;

        var tcs = new TaskCompletionSource<string?>();

        okButton.Click += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(string.IsNullOrWhiteSpace(textBox.Text) ? null : textBox.Text.Trim());
            window.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);
            window.Close();
        };

        window.Closing += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);
        };

        Window? parent = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            parent = desktop.MainWindow;

        if (parent is null)
            window.Show();
        else
            await window.ShowDialog(parent);

        return await tcs.Task;
    }

    public async Task<Playlist?> ShowPlaylistEditorAsync(Playlist? playlist)
    {
        var window = new Window
        {
            Title = playlist is null ? "Создать плейлист" : "Редактировать плейлист",
            Width = 480,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var nameBox = new TextBox { Watermark = "Название плейлиста", Text = playlist?.Name ?? string.Empty, Margin = new Thickness(0,0,0,8) };
        Grid.SetRow(nameBox, 0);

        var descBox = new TextBox { Watermark = "Описание (опционально)", Text = playlist?.Description ?? string.Empty, AcceptsReturn = true };
        Grid.SetRow(descBox, 1);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        Grid.SetRow(buttons, 2);
        var ok = new Button { Content = "OK", Width = 90 };
        var cancel = new Button { Content = "Отмена", Width = 90 };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        grid.Children.Add(nameBox);
        grid.Children.Add(descBox);
        grid.Children.Add(buttons);

        window.Content = grid;

        var tcs = new TaskCompletionSource<Playlist?>();

        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text)) return;

            var result = new Playlist
            {
                Id = playlist?.Id ?? Guid.NewGuid(),
                Name = nameBox.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(descBox.Text) ? null : descBox.Text.Trim()
            };

            if (!tcs.Task.IsCompleted) tcs.SetResult(result);
            window.Close();
        };

        cancel.Click += (_, _) =>
        {
            if (!tcs.Task.IsCompleted) tcs.SetResult(null);
            window.Close();
        };

        window.Closing += (_, _) => { if (!tcs.Task.IsCompleted) tcs.SetResult(null); };

        Window? parent = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            parent = desktop.MainWindow;

        if (parent is null)
            window.Show();
        else
            await window.ShowDialog(parent);

        return await tcs.Task;
    }
}

