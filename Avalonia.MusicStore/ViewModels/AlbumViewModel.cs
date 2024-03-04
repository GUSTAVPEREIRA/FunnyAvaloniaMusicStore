using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.MusicStore.Models;
using ReactiveUI;
using SkiaSharp;

namespace Avalonia.MusicStore.ViewModels;

public class AlbumViewModel(Album album) : ViewModelBase
{
    public string Artist => album.Artist;
    public string Title => album.Title;
    private Bitmap? _cover;

    public async Task SaveToDiskAsync()
    {
        await album.SaveAsync();

        if (Cover != null)
        {
            var bitmap = Cover;

            await Task.Run(() =>
            {
                using var file = album.SaveCoverBitmapStream();
                bitmap.Save(file);
            });
        }
    }

    public Bitmap? Cover
    {
        get => _cover;
        private set => this.RaiseAndSetIfChanged(ref _cover, value);
    }

    public async Task LoadCover()
    {
        await using var imageStream = await album.LoadCoverBitmapAsync();

        using var bitmapDecoded = SKBitmap.Decode(imageStream);
        var realScale = CalculateRealScale(bitmapDecoded);
        using var resized = bitmapDecoded.Resize(new SKSizeI(200, realScale * 200), SKFilterQuality.High);

        if (resized == null)
        {
            return;
        }

        using var encode = resized.Encode(SKEncodedImageFormat.Webp, 100);
        using var memoryStream = new MemoryStream();
        encode.SaveTo(memoryStream);
        memoryStream.Position = 0;
        Cover = new Bitmap(memoryStream);
    }

    private static int CalculateRealScale(SKBitmap bitmapDecoded)
    {
        if (bitmapDecoded.Height < bitmapDecoded.Width)
        {
            return bitmapDecoded.Height / bitmapDecoded.Width;
        }

        return bitmapDecoded.Width / bitmapDecoded.Height;
    }
}