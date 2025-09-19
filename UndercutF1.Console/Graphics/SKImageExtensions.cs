using SkiaSharp;

namespace UndercutF1.Console.Graphics;

internal static class SKImageExtensions
{
    public static string[] ToGraphicsSequence(
        this SKImage image,
        TerminalInfoProvider terminalInfo,
        int windowHeight,
        int windowWidth
    )
    {
        if (terminalInfo.IsITerm2ProtocolSupported.Value)
        {
            var imageData = image.Encode();
            var base64 = Convert.ToBase64String(imageData.AsSpan());
            return [TerminalGraphics.ITerm2GraphicsSequence(windowHeight, windowWidth, base64)];
        }
        else if (terminalInfo.IsKittyProtocolSupported.Value)
        {
            var imageData = image.Encode();
            var base64 = Convert.ToBase64String(imageData.AsSpan());
            return
            [
                TerminalGraphics.KittyGraphicsSequenceDelete(),
                .. TerminalGraphics.KittyGraphicsSequence(windowHeight, windowWidth, base64),
            ];
        }
        else if (terminalInfo.IsSixelSupported.Value)
        {
            var bitmap = SKBitmap.FromImage(image);
            var sixelData = Sixel.ImageToSixel(bitmap.Pixels, bitmap.Width);
            return [TerminalGraphics.SixelGraphicsSequence(sixelData)];
        }

        return
        [
            "Unexpected error whilst creating graphics, we shouldn't have got here. Please report!",
        ];
    }
}
