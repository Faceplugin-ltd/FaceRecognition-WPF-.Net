using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;

public class ImageProcessor
{
    public ImageProcessor()
    {

    }
    private static Bitmap ConvertTo24bpp(System.Drawing.Image img)
    {
        var bmp = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        using (var gr = Graphics.FromImage(bmp))
            gr.DrawImage(img, new System.Drawing.Rectangle(0, 0, img.Width, img.Height));
        return bmp;
    }

    private static System.Drawing.Image LoadImageWithExif(String filePath)
    {
        try
        {
            System.Drawing.Image image = System.Drawing.Image.FromFile(filePath);

            // Check if the image has EXIF orientation data
            if (image.PropertyIdList.Contains(0x0112))
            {
                int orientation = image.GetPropertyItem(0x0112).Value[0];

                switch (orientation)
                {
                    case 1:
                        // Normal
                        break;
                    case 3:
                        // Rotate 180
                        image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 6:
                        // Rotate 90
                        image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 8:
                        // Rotate 270
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                    default:
                        // Do nothing
                        break;
                }
            }

            return image;
        }
        catch (Exception e)
        {
            throw new Exception("Image null!");
        }
    }

    // Function to load an image, convert it to 24bpp, and extract pixel data as a byte array
    public static (byte[], int, int, int) ProcessImage(string imagePath)
    {
        System.Drawing.Image image = null;
        try
        {
            image = LoadImageWithExif(imagePath);
        }
        catch (Exception)
        {
            System.Windows.MessageBox.Show("Unknown Image Format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        Bitmap imgBmp = ConvertTo24bpp(image);
        BitmapData bitmapData = imgBmp.LockBits(new System.Drawing.Rectangle(0, 0, imgBmp.Width, imgBmp.Height), ImageLockMode.ReadWrite, imgBmp.PixelFormat);

        int bytesPerPixel = Bitmap.GetPixelFormatSize(imgBmp.PixelFormat) / 8;
        int byteCount = bitmapData.Stride * imgBmp.Height;
        byte[] pixels = new byte[byteCount];
        IntPtr ptrFirstPixel = bitmapData.Scan0;
        Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

        imgBmp.UnlockBits(bitmapData);

        return (pixels, imgBmp.Width, imgBmp.Height, bitmapData.Stride);
    }
}