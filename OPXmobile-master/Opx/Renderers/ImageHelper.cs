using Microsoft.Maui.Graphics.Platform;

namespace Opx.Renderers
{
    public static class ImageHelper
    {
        private const int MaxImageSize = 1024; // Max width/height in pixels
        private const int JpegQuality = 85; // JPEG quality (0-100)

        public static async Task<string> ResizeAndSaveImageAsync(Stream imageStream, string fileName, string cacheDirectory)
        {
            try
            {
                // Create cache directory if it doesn't exist
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                var outputPath = Path.Combine(cacheDirectory, fileName);

                // Load the image
                using (var image = PlatformImage.FromStream(imageStream))
                {
                    if (image == null)
                        throw new InvalidOperationException("Failed to load image");

                    // Calculate new dimensions
                    var (newWidth, newHeight) = CalculateNewDimensions((int)image.Width, (int)image.Height, MaxImageSize);

                    // Resize the image
                    var resizedImage = image.Resize(newWidth, newHeight, ResizeMode.Fit);

                    // Save as JPEG
                    using (var outputStream = File.Create(outputPath))
                    {
                        resizedImage.Save(outputStream, ImageFormat.Jpeg);
                    }

                    resizedImage.Dispose();
                }

                return outputPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Image resize error: {ex.Message}");
                throw;
            }
        }

        private static (int width, int height) CalculateNewDimensions(int originalWidth, int originalHeight, int maxSize)
        {
            if (originalWidth <= maxSize && originalHeight <= maxSize)
                return (originalWidth, originalHeight);

            double ratio = Math.Min((double)maxSize / originalWidth, (double)maxSize / originalHeight);
            return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
        }

        public static async Task<bool> IsValidImageAsync(Stream imageStream)
        {
            try
            {
                imageStream.Position = 0;
                using (var image = PlatformImage.FromStream(imageStream))
                {
                    return image != null && image.Width > 0 && image.Height > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public static long GetStreamSize(Stream stream)
        {
            try
            {
                return stream.Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}