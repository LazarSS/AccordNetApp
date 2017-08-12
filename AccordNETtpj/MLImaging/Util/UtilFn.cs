using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MLImaging.Util
{
    class UtilFn
    {

        static public Bitmap getBmpFromCvsim(System.Windows.Controls.Image cvsim)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)cvsim.ActualWidth, (int)cvsim.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(cvsim);

            MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            encoder.Save(stream);
            return new Bitmap(stream);
        }

    }
}
