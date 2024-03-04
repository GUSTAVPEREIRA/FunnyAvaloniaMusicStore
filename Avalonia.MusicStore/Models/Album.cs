using iTunesSearch.Library;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Avalonia.MusicStore.Models;

public class Album(string artist, string title, string coverUrl)
{
    private static readonly iTunesSearchManager TunesSearchManager = new();
    private static readonly HttpClient HttpClient = new();
    private string CachePath => $"{DirectoryCachePath}/{Artist} - {Title}";
    private const string DirectoryCachePath = "./Cache";
    public string Artist { get; set; } = artist;
    public string Title { get; set; } = title;
    private string CoverUrl { get; set; } = coverUrl;

    public static async Task<IEnumerable<Album>> SearchAsync(string searchTerm)
    {
        var query = await TunesSearchManager.GetAlbumsAsync(searchTerm);

        return query.Albums.Select(x =>
            new Album(x.ArtistName, x.CollectionName, x.ArtworkUrl100.Replace("100x100bb", "600x600bb")));
    }

    public async Task SaveAsync()
    {
        if (!Directory.Exists(DirectoryCachePath))
        {
            Directory.CreateDirectory(DirectoryCachePath);
        }

        await using var file = File.OpenWrite(CachePath);
        await SaveToStreamAsync(this, file);
    }

    public static async Task<Album> LoadFromStream(Stream stream)
    {
        return (await JsonSerializer.DeserializeAsync<Album>(stream).ConfigureAwait(false))!;
    }

    public static async Task<IEnumerable<Album>> LoadCachedAsync()
    {
        if (!Directory.Exists(DirectoryCachePath))
        {
            Directory.CreateDirectory(DirectoryCachePath);
        }

        var results = new List<Album>();

        foreach (var file in Directory.EnumerateFiles(DirectoryCachePath))
        {
            if (!string.IsNullOrWhiteSpace(new DirectoryInfo(file).Extension))
            {
                continue;
            }

            await using var fs = File.OpenRead(file);
            results.Add(await LoadFromStream(fs).ConfigureAwait(false));
        }

        return results;
    }

    public Stream SaveCoverBitmapStream()
    {
        return File.OpenWrite(CachePath + ".bmp");
    }

    public async Task<Stream> LoadCoverBitmapAsync()
    {
        var cachePathBitmap = CachePath + ".bmp";
        if (File.Exists(cachePathBitmap))
        {
            return File.OpenRead(cachePathBitmap);
        }

        var data = await HttpClient.GetByteArrayAsync(CoverUrl);
        return new MemoryStream(data);
    }

    private static async Task SaveToStreamAsync(Album data, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false);
    }
}