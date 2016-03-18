using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace rcImageMerge
{
    class Program
    {
        static void Main(string[] args)
        {
            MergeInfo[] execParams = LoadParams(args);
            ProcessBitmaps(execParams);
        }

        public static MergeInfo[] LoadParams(string[] args)
        {
            List<MergeInfo> files = new List<MergeInfo>();

            foreach (string s in args)
            {
                if (s.ToUpper().StartsWith("/TARGET:"))
                {
                    files.Add(
                        new MergeInfo
                        {
                            Alpha = 1,
                            Filename = s,
                            Origen = new Point(0, 0)
                        }
                        );
                }
                else
                {
                    string[] parts = s.Split(',');
                    if (File.Exists(parts[0]))
                    {
                        MergeInfo item = new MergeInfo()
                        {
                            Alpha = 1,
                            Filename = parts[0],
                            Origen = new Point(0, 0)
                        };
                        if (parts.Length >= 3)
                        {
                            item.Origen.X = int.Parse(parts[1]);
                            item.Origen.Y = int.Parse(parts[2]);
                        }
                        if (parts.Length >= 4)
                        {
                            item.Alpha = float.Parse(parts[3]) / 100.0f;
                        }
                        if (parts.Length >= 5)
                        {
                            item.Transparent = (int.Parse(parts[4]) == 1);
                        }
                        files.Add(item);
                    }
                }
            }

            return files.ToArray();
        }

        public static void ConvertBitmap(ref Bitmap not32Bit)
        {
            Bitmap tmp = new Bitmap(not32Bit.Width, not32Bit.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(tmp);
            //g.DrawImageUnscaled(Not32Bit, 0, 0);
            g.DrawImage(not32Bit, new Point(0, 0));
            not32Bit.Dispose();
            not32Bit = tmp;
        }

        public static void ProcessBitmaps(MergeInfo[] mergeFiles)
        {
            //Debugger.Break();
            Graphics target = null;
            Bitmap file = null;
            string saveFileName = "";

            foreach (MergeInfo info in mergeFiles)
            {
                if (!info.Filename.ToUpper().StartsWith("/TARGET:"))
                {
                    if (target == null)
                    {
                        using (FileStream fs = new FileStream(info.Filename, FileMode.Open, FileAccess.Read))
                        {
                            file = new Bitmap(fs);
                        }
                        ConvertBitmap(ref file);
                        target = Graphics.FromImage(file);
                    }
                    else
                    {
                        ColorMatrix cm = new ColorMatrix { Matrix33 = info.Alpha };
                        ImageAttributes ia = new ImageAttributes();
                        ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                        Bitmap current;
                        using (FileStream fs = new FileStream(info.Filename, FileMode.Open, FileAccess.Read))
                        {
                            current = new Bitmap(fs);
                            ConvertBitmap(ref current);
                        }

                        if (info.Transparent) current.MakeTransparent();
                        target.DrawImage(
                           current,
                           new Rectangle(info.Origen.X, info.Origen.Y, current.Width, current.Height),
                           0, 0, current.Width, current.Height,
                           GraphicsUnit.Pixel,
                           ia);

                        current.Dispose();
                        ia.Dispose();
                    }
                }
                else
                {
                    saveFileName = info.Filename.Substring(8);
                }

            }

            var path = Path.GetDirectoryName(saveFileName);
            if ((file != null) && (path!=null && Directory.Exists(path)))
            {
                ImageFormat fmt;
                string ext = Path.GetExtension(saveFileName.ToUpper());
                switch (ext)
                {
                    case "JPEG":
                    case "JPG":
                        fmt = ImageFormat.Jpeg;
                        break;
                    case "PNG":
                        fmt = ImageFormat.Png;
                        break;
                    case "GIF":
                        fmt = ImageFormat.Gif;
                        break;
                    case "TIF":
                    case "TIFF":
                        fmt = ImageFormat.Tiff;
                        break;
                    default:
                        fmt = ImageFormat.Bmp;
                        break;
                }

                using (FileStream fs = new FileStream(saveFileName, FileMode.Create, FileAccess.ReadWrite))
                {
                    file.Save(fs, fmt);
                }
            }

        }
    }
}
