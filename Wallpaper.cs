using System;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Service.Wallpaper;
using Android.Views;
using FlickrNet;
using System.Net.Http;
using Android.Renderscripts;
using System.Threading.Tasks;
using Android.Runtime;
using Android.Widget;
using Android.Content;
//using Android.Util;

namespace FlickrLiveWallpaper
{
    [Service(Label = "@string/app_name", Permission = "android.permission.BIND_WALLPAPER")]
    [IntentFilter(new string[] { "android.service.wallpaper.WallpaperService" })]
    [MetaData("android.service.wallpaper", Resource = "@xml/wallpaper")]
    class Wallpaper : WallpaperService
    {
        public override WallpaperService.Engine OnCreateEngine()
        {
            return new FlickrEngine(this);
        }

        class FlickrEngine : WallpaperService.Engine
        {
            //private LockScreenVisibleReceiver mLockScreenVisibleReceiver;

            // private bool is_visible;

            //private Handler mHandler = new Handler();
            //private Action mDrawFrame;

            private float xoffset;
            private float yoffset;
            private bool UseWallpaper;

            public FlickrEngine(Wallpaper wall) : base(wall)
            {
                //mDrawFrame = delegate { DrawFrame(); };
            }
            
            public override void OnCreate(ISurfaceHolder surfaceHolder)
            {
                base.OnCreate(surfaceHolder);
                //mLockScreenVisibleReceiver = new LockScreenVisibleReceiver();
                //mLockScreenVisibleReceiver.setupRegisterDeregister(Application.Context);

                // DrawFrame();
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
                /*
                if (mLockScreenVisibleReceiver != null)
                {
                    mLockScreenVisibleReceiver.Destroy();
                    mLockScreenVisibleReceiver = null;
                }
                */
                //mHandler.RemoveCallbacks(mDrawFrame);
            }



            /*
             *  "It is very important that a wallpaper only use CPU while it is visible.. "
             *  
             */
            public override void OnVisibilityChanged(bool visible)
            {
                //is_visible = visible;

                if (visible)
                    DrawFrame();
                //else
                //    mHandler.RemoveCallbacks (mDrawFrame);
            }
            // */

            // "This method is always called at least once, after surfaceCreated(SurfaceHolder)."
            public override void OnSurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                base.OnSurfaceChanged(holder, format, width, height);
                /*
                mScreenHeight = height;
                mScreenWidth = width;
                */
                DrawFrame();
            }

            // "After returning from this call, you should no longer try to access this surface."
            public override void OnSurfaceDestroyed(ISurfaceHolder holder)
            {
                base.OnSurfaceDestroyed(holder);
                //is_visible = false;
                //mHandler.RemoveCallbacks(mDrawFrame);
            }

            private DateTime lastUpdate;
            private int mTotal = -1;

            async Task<Bitmap> GetBmp()
            {

                Flickr f = MyFlickr.getFlickr();
                PhotoSearchOptions pso = new PhotoSearchOptions("77788903@N00", "myfavs");
                // pso.Extras = PhotoSearchExtras.OriginalFormat;
                pso.PerPage = 1;
                if (mTotal < 1)
                {
                    var dum = await f.PhotosSearchAsync(pso);
                    mTotal = dum.Total;
                }
                pso.Page = new Random().Next(1, mTotal); // mIndex++;
                PhotoCollection pc = await f.PhotosSearchAsync(pso);

                mTotal = pc.Total;

                /*
                if (pc.Total == mIndex)
                    mIndex = 0;
                */

                var wallpaperManager = WallpaperManager.GetInstance(Application.Context);
                var targetHeight = (float)wallpaperManager.DesiredMinimumHeight / 1.5;
                var targetWidth = (float)wallpaperManager.DesiredMinimumWidth / 1.5;

                SizeCollection sc = await f.PhotosGetSizesAsync(pc[0].PhotoId);
                Size max = null;
                foreach (Size s in sc)
                {
                    // Always set if we haven't got a size already
                    if (max == null)
                        max = s;
                    // Set if this height/width is bigger than the one we have and the one we have is less than target
                    else if ((s.Height > max.Height) && (max.Height < targetHeight))
                        max = s;
                    else if ((s.Width > max.Width) && (max.Width < targetWidth))
                        max = s;
                    // Set if this height/width is bigger than target, but smaller than the one we have. ie, get the smallest picture that's big enough
                    else if ((s.Height > targetHeight) && (s.Height < max.Height))
                        max = s;
                    else if ((s.Width > targetWidth) && (s.Width < max.Width))
                        max = s;
                }

                string url = max.Source;

                using (HttpClient hc = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage response = await hc.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    BitmapFactory.Options opts = new BitmapFactory.Options();
                    opts.InMutable = true;
                    var bmp = BitmapFactory.DecodeStream(await response.Content.ReadAsStreamAsync(), null, opts);

                    Canvas c = new Canvas(bmp);

                    Paint p = new Paint();
                    p.Alpha = 255;
                    p.AntiAlias = true;

                    p.Color = Color.Yellow;
                    p.TextSize = 20;

                    // bmp.Width + " " + bmp.Height + " " + DateTime.Now

                    c.DrawText(bmp.Width + " " + bmp.Height + " " + DateTime.Now, bmp.Width / 2, bmp.Height / 2, p);

                    return bmp;
                }
            }

            Bitmap GbmpWallpaper;

            // This method gets called repeatedly
            // by posting a delayed Runnable.
            async void DrawFrame()
            {
                try
                {
                    if (/*(GbmpWallpaper == null) ||*/ (lastUpdate == null) || (lastUpdate.AddHours(Settings.IntervalHours) < DateTime.Now))
                    {
                        lastUpdate = DateTime.Now;

                        /*Bitmap*/
                        GbmpWallpaper = await GetBmp();

                        UseWallpaper = Settings.UseWallpaper;

                        if (UseWallpaper)
                            UseManager(GbmpWallpaper);
                    }

                    if (!UseWallpaper)
                        UseCanvas(GbmpWallpaper);
                }
                catch
                {

                }

                //mHandler.RemoveCallbacks(mDrawFrame);

                // if (is_visible)
                //mHandler.PostDelayed(mDrawFrame, (long)(lastUpdate.AddHours(Settings.IntervalHours) - DateTime.Now).TotalMilliseconds);
            }

            private void UseManager(Bitmap bmpWallpaper)
            {
                
                Bitmap bmp = bmpWallpaper;
                var wallpaperManager = WallpaperManager.GetInstance(Application.Context);
                //var txt = "";
/*
                var hRat = (float)wallpaperManager.DesiredMinimumHeight / bmpWallpaper.Height / 2;
                var wRat = (float)wallpaperManager.DesiredMinimumWidth / bmpWallpaper.Width / 2;
                var scale = Math.Max(wRat, hRat);
                txt = scale.ToString();
                if (scale > 1)
                {
                    var s = Math.Min(scale, 1.5);
                    var width = (int)Math.Round(bmpWallpaper.Width * s);
                    var height = (int)Math.Round(bmpWallpaper.Height * s);
                    //var hDiff = (c.Height - height) / 2;
                    //var wDiff = (c.Width - width) / 2;
                    bmp = Bitmap.CreateScaledBitmap(bmpWallpaper, width, height, true);
                }
                else
                {
                    bmp = bmpWallpaper;
                    //var config = Bitmap.Config.Rgb565;
                    //bmp = bmpWallpaper.Copy(config, true);
                }
*/
/*
                Canvas c = new Canvas(bmp);

                Paint p = new Paint();
                p.Alpha = 255;
                p.AntiAlias = true;

                p.Color = Color.Yellow;
                p.TextSize = 30;
                
                // bmp.Width + " " + bmp.Height + " " + DateTime.Now

                c.DrawText(lastUpdate.AddHours(Settings.IntervalHours).ToString(), bmp.Width / 4, bmp.Height / 2, p);
                */
                wallpaperManager.SetBitmap(bmp);
            }

            public override void OnOffsetsChanged(float xOffset, float yOffset, float xOffsetStep, float yOffsetStep, int xPixelOffset, int yPixelOffset)
            {
                xoffset = xOffset;
                yoffset = yOffset;
                if (!UseWallpaper)
                    UseCanvas(GbmpWallpaper);
            }

            private async void UseCanvas(Bitmap bmpWallpaper)
            {
                ISurfaceHolder holder = SurfaceHolder;

                Canvas c = null;

                try
                {
                    c = holder.LockCanvas();

                    if (c != null)
                    {
                        string txt = "-";

                        try
                        {
                            //const float MAX_RATIO = 2f;
                            var hRat = (float)c.Height / bmpWallpaper.Height;
                            var wRat = 1.5 * c.Width / bmpWallpaper.Width; // 1.5 - arbitrary minimun canvas width multiplier for scrolling
                            var scale = Math.Max(wRat, hRat);
                            var width = (int)Math.Round(bmpWallpaper.Width * scale);
                            var height = (int)Math.Round(bmpWallpaper.Height * scale);
                            var hDiff = (c.Height - height) / 2;
                            // var wDiff = (c.Width - width) / 2;
                            //var left = (int)Math.Round((c.Width - width) * xoffset);
                            var wDiff = (Math.Min(scale, 2) - 1) * c.Width; // 2 - arbitrary maximum canvas width multiplier for scrolling
                            var left = (int)Math.Round(wDiff - (width / 2) - (wDiff * xoffset));

                            // var top = (int)Math.Round((c.Height - height) * yoffset);

                            var src = new Rect(0, 0, bmpWallpaper.Width - 1, bmpWallpaper.Height - 1);
                            var dst = new Rect(left, hDiff, width + left, height + hDiff);

                            txt = xoffset + " " + left + " " + DateTime.Now; // + " " + mLockScreenVisibleReceiver.LockScreenVisible;

                            if (true) // mLockScreenVisibleReceiver.LockScreenVisible)
                                c.DrawBitmap(bmpWallpaper, src, dst, null);
                            else
                                await Task.Factory.StartNew(() =>
                            {
                                return CreateBlurredImage(bmpWallpaper.Copy(bmpWallpaper.GetConfig(), false), 10);
                            }).ContinueWith(task =>
                            {
                                Bitmap res = task.Result;
                                c.DrawBitmap(res, src, dst, null);
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                            // */
                            // }

                        }
                        catch (Exception ex)
                        {
                            txt += System.Environment.NewLine + ex.Message;

                        }

                        Paint p = new Paint();
                        p.Alpha = 255;
                        p.AntiAlias = true;
                        p.Color = Color.Yellow;
                        p.TextSize = 40;

                        float w = p.MeasureText(txt, 0, txt.Length);
                        int aoffset = (int)w / 2;
                        int x = c.Width / 2 - aoffset;
                        int y = c.Height / 3;

                        c.DrawText(txt, x, y, p);

                    }
                }
                finally
                {
                    if (c != null)
                        holder.UnlockCanvasAndPost(c);
                }
            }

            private Bitmap CreateBlurredImage(Bitmap originalBitmap, int radius)
            {
                // originalBitmap

                // Create another bitmap that will hold the results of the filter.
                Bitmap blurredBitmap = Bitmap.CreateBitmap(originalBitmap);

                // Create the Renderscript instance that will do the work.
                RenderScript rs = RenderScript.Create(Application.Context);

                // Allocate memory for Renderscript to work with
                Allocation input = Allocation.CreateFromBitmap(rs, originalBitmap, Allocation.MipmapControl.MipmapFull, AllocationUsage.Script);
                Allocation output = Allocation.CreateTyped(rs, input.Type);

                // Load up an instance of the specific script that we want to use.
                ScriptIntrinsicBlur script = ScriptIntrinsicBlur.Create(rs, Element.U8_4(rs));
                script.SetInput(input);

                // Set the blur radius
                script.SetRadius(radius);

                // Start the ScriptIntrinisicBlur
                script.ForEach(output);

                // Copy the output to the blurred bitmap
                output.CopyTo(blurredBitmap);

                return blurredBitmap;
            }
        }
    }
}

