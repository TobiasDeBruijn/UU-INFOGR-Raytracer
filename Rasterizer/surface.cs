using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Rasterizer; 

public class Surface {
    public readonly int Width;
    public readonly int Height;
    public readonly int[] Pixels;
    private static Surface? _font;

    private static int[]? _fontRedir;

    // surface constructor
    public Surface(int w, int h) {
        Width = w;
        Height = h;
        Pixels = new int[w * h];
    }

    // surface constructor using a file
    private Surface(string fileName) {
        Image<Bgra32> bmp = Image.Load<Bgra32>(fileName);
        Width = bmp.Width;
        Height = bmp.Height;
        Pixels = new int[Width * Height];
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            Pixels[y * Width + x] = (int)bmp[x, y].Bgra;
    }

    // create an OpenGL texture
    public int GenTexture() {
        int id = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, id);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Bgra,
            PixelType.UnsignedByte, Pixels);
        return id;
    }

    // clear the surface
    public void Clear(int c) {
        for (int s = Width * Height, p = 0; p < s; p++) Pixels[p] = c;
    }
    
    // print a string
    public void Print(string t, int x, int y, int c) {
        if (_font == null || _fontRedir == null) {
            _font = new Surface("../../../assets/font.png");
            const string ch = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+={}[];:<>,.?/\\ ";
            _fontRedir = new int[256];
            for (int i = 0; i < 256; i++) _fontRedir[i] = 0;
            for (int i = 0; i < ch.Length; i++) {
                int l = ch[i];
                _fontRedir[l & 255] = i;
            }
        }

        for (int i = 0; i < t.Length; i++) {
            int f = _fontRedir[t[i] & 255];
            int dest = x + i * 12 + y * Width;
            int src = f * 12;
            for (int v = 0; v < _font.Height; v++, src += _font.Width, dest += Width)
            for (int u = 0; u < 12; u++)
                if ((_font.Pixels[src + u] & 0xffffff) != 0)
                    Pixels[dest + u] = c;
        }
    }
}