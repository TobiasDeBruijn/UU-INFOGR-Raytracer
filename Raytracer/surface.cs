using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using OpenTK.Graphics.OpenGL;

namespace Template
{
    public class Surface
    {
        public int width, height;
        public int[] pixels;
        private static Surface? _font;

        private static int[]? _fontRedir;
        // surface constructor
        public Surface(int w, int h)
        {
            width = w;
            height = h;
            pixels = new int[w * h];
        }
        // surface constructor using a file
        public Surface(string fileName)
        {
            Image<Bgra32> bmp = Image.Load<Bgra32>(fileName);
            width = bmp.Width;
            height = bmp.Height;
            pixels = new int[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = (int)bmp[x, y].Bgra;
        }
        // create an OpenGL texture
        public int GenTexture()
        {
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);
            return id;
        }
        // clear the surface
        public void Clear(int c)
        {
            for (int s = width * height, p = 0; p < s; p++) pixels[p] = c;
        }
        // copy the surface to another surface
        // draw a rectangle
        // draw a solid bar
        // helper function for line clipping
        private int Outcode(int x, int y)
        {
            int xmin = 0, ymin = 0, xmax = width - 1, ymax = height - 1;
            return ((x < xmin) ? 1 : ((x > xmax) ? 2 : 0)) + ((y < ymin) ? 4 : ((y > ymax) ? 8 : 0));
        }
        // draw a line, clipped to the window
        public void Line(int x1, int y1, int x2, int y2, int c)
        {
            int xmin = 0, ymin = 0, xmax = width - 1, ymax = height - 1;
            int c0 = Outcode(x1, y1), c1 = Outcode(x2, y2);
            bool accept = false;
            while (true) {
                if (c0 == 0 && c1 == 0) {
                    accept = true; 
                    break;
                }
                if ((c0 & c1) > 0) break;
                int x = 0, y = 0;
                int co = (c0 > 0) ? c0 : c1;
                if ((co & 8) > 0) { x = x1 + (x2 - x1) * (ymax - y1) / (y2 - y1); y = ymax; }
                else if ((co & 4) > 0) { x = x1 + (x2 - x1) * (ymin - y1) / (y2 - y1); y = ymin; }
                else if ((co & 2) > 0) { y = y1 + (y2 - y1) * (xmax - x1) / (x2 - x1); x = xmax; }
                else if ((co & 1) > 0) { y = y1 + (y2 - y1) * (xmin - x1) / (x2 - x1); x = xmin; }
                if (co == c0) { x1 = x; y1 = y; c0 = Outcode(x1, y1); }
                else { x2 = x; y2 = y; c1 = Outcode(x2, y2); }
            }
            if (!accept) return;
            if (Math.Abs(x2 - x1) >= Math.Abs(y2 - y1))
            {
                if (x2 < x1) { (x2, x1) = (x1, x2); (y2, y1) = (y1, y2); }
                int l = x2 - x1;
                if (l == 0) return;
                int dy = ((y2 - y1) * 8192) / l;
                y1 *= 8192;
                for (int i = 0; i < l; i++)
                {
                    pixels[x1++ + (y1 / 8192) * width] = c;
                    y1 += dy;
                }
            }
            else
            {
                if (y2 < y1) { (x2, x1) = (x1, x2); (y2, y1) = (y1, y2); }
                int l = y2 - y1;
                if (l == 0) return;
                int dx = ((x2 - x1) * 8192) / l;
                x1 *= 8192;
                for (int i = 0; i < l; i++)
                {
                    pixels[x1 / 8192 + y1++ * width] = c;
                    x1 += dx;
                }
            }
        }
        // plot a single pixel
        // print a string
        public void Print(string t, int x, int y, int c)
        {
            if (_font == null || _fontRedir == null)
            {
                _font = new Surface("../../../assets/font.png");
                string ch = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+={}[];:<>,.?/\\ ";
                _fontRedir = new int[256];
                for (int i = 0; i < 256; i++) _fontRedir[i] = 0;
                for (int i = 0; i < ch.Length; i++)
                {
                    int l = (int)ch[i];
                    _fontRedir[l & 255] = i;
                }
            }
            for (int i = 0; i < t.Length; i++)
            {
                int f = _fontRedir[(int)t[i] & 255];
                int dest = x + i * 12 + y * width;
                int src = f * 12;
                for (int v = 0; v < _font.height; v++, src += _font.width, dest += width) for (int u = 0; u < 12; u++)
                {
                    if ((_font.pixels[src + u] & 0xffffff) != 0) pixels[dest + u] = c;
                }
            }
        }
    }
}
