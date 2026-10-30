using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ShakeCursor;

/// <summary>Renders a vector arrow cursor with a soft shadow at a given height (physical pixels) and caches it by height.</summary>
sealed class ArrowCursor : IDisposable
{
    // Arrow outline, in units where the arrow is 100 tall
    static readonly PointF[] Shape =
    [
        new(0, 0), new(0, 79), new(19, 62), new(31, 92), new(45, 86), new(33, 58), new(57, 58),
    ];
    const float ShapeWidth = 57, ShapeHeight = 92;

    readonly Dictionary<int, IntPtr> cache = new();

    /// <summary>Gets (or creates) the cursor of the given height. The handle is owned by this object; do not destroy it.</summary>
    public IntPtr Get(int height)
    {
        if (!cache.TryGetValue(height, out var h))
        {
            if (cache.Count > 256)
                Clear();
            h = Create(height);
            cache[height] = h;
        }
        return h;
    }

    public void Clear()
    {
        foreach (var h in cache.Values)
            Native.DestroyIcon(h);
        cache.Clear();
    }

    public void Dispose() => Clear();

    /// <summary>Draws the arrow into a bitmap (also used for the tray icon). tip is where the arrow tip lands.</summary>
    public static Bitmap Render(int height, out Point tip)
    {
        var scale = height / ShapeHeight;
        var stroke = Math.Max(1.5f, height / 22f);
        var shadow = height / 24f;
        var pad = (int)Math.Ceiling(stroke / 2 + shadow * 2) + 1;
        var width = (int)Math.Ceiling(ShapeWidth * scale) + pad * 2;
        var bmpHeight = height + pad * 2;

        var bmp = new Bitmap(width, bmpHeight, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        using var path = new GraphicsPath();
        path.AddPolygon(Shape.Select(p => new PointF(pad + p.X * scale, pad + p.Y * scale)).ToArray());

        // Soft shadow: offset down-right and built from a few widening translucent strokes
        if (height >= 24)
        {
            using var shadowPath = (GraphicsPath)path.Clone();
            using (var m = new Matrix())
            {
                m.Translate(shadow * 0.4f, shadow);
                shadowPath.Transform(m);
            }
            for (var i = 4; i >= 1; i--)
            {
                using var pen = new Pen(Color.FromArgb(14, 0, 0, 0), stroke + shadow * i / 2) { LineJoin = LineJoin.Round };
                g.DrawPath(pen, shadowPath);
            }
            using var shadowFill = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            g.FillPath(shadowFill, shadowPath);
        }

        g.FillPath(Brushes.White, path);
        using (var outline = new Pen(Color.Black, stroke) { LineJoin = LineJoin.Round })
            g.DrawPath(outline, path);

        tip = new Point(pad, pad);
        return bmp;
    }

    static IntPtr Create(int height)
    {
        using var bmp = Render(height, out var tip);
        return ToCursor(bmp, tip);
    }

    /// <summary>32-bit DIB with alpha plus an all-zero mask, turned into a cursor handle.</summary>
    static unsafe IntPtr ToCursor(Bitmap bmp, Point hotspot)
    {
        int w = bmp.Width, h = bmp.Height;
        var header = new Native.BITMAPV5HEADER
        {
            bV5Size = (uint)Marshal.SizeOf<Native.BITMAPV5HEADER>(),
            bV5Width = w,
            bV5Height = -h, // top-down
            bV5Planes = 1,
            bV5BitCount = 32,
            bV5Compression = 3, // BI_BITFIELDS
            bV5RedMask = 0x00FF0000,
            bV5GreenMask = 0x0000FF00,
            bV5BlueMask = 0x000000FF,
            bV5AlphaMask = 0xFF000000,
        };
        var color = Native.CreateDIBSection(IntPtr.Zero, ref header, 0, out var bits, IntPtr.Zero, 0);
        var mask = Native.CreateBitmap(w, h, 1, 1, IntPtr.Zero);
        try
        {
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (var y = 0; y < h; y++)
                    Buffer.MemoryCopy((byte*)data.Scan0 + y * data.Stride, (byte*)bits + y * w * 4, w * 4, w * 4);
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            var info = new Native.ICONINFO
            {
                fIcon = false,
                xHotspot = hotspot.X,
                yHotspot = hotspot.Y,
                hbmMask = mask,
                hbmColor = color,
            };
            return Native.CreateIconIndirect(ref info);
        }
        finally
        {
            Native.DeleteObject(color);
            Native.DeleteObject(mask);
        }
    }

    /// <summary>Visible height of the current system arrow (physical pixels). Growth starts from it, so there is no jump.</summary>
    public static int MeasureSystemArrowHeight()
    {
        // Draw it onto a magenta canvas; the rows that changed are the arrow
        const int canvas = 512;
        var key = Color.Magenta.ToArgb();
        using var bmp = new Bitmap(canvas, canvas, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Magenta);
            var hdc = g.GetHdc();
            try
            {
                Native.DrawIconEx(hdc, 0, 0, Native.LoadCursor(IntPtr.Zero, Native.IDC_ARROW), 0, 0, 0, IntPtr.Zero, Native.DI_NORMAL);
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }

        var data = bmp.LockBits(new Rectangle(0, 0, canvas, canvas), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var pixels = new int[canvas * canvas];
        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        bmp.UnlockBits(data);

        int top = -1, bottom = -1;
        for (var y = 0; y < canvas; y++)
        {
            if (Array.FindIndex(pixels, y * canvas, canvas, p => p != key) < 0) continue;
            if (top < 0) top = y;
            bottom = y;
        }
        return top < 0 ? (int)Math.Round(SystemInformation.CursorSize.Height * 0.62) : bottom - top + 1;
    }
}
