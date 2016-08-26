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
//using Android.Widget;
using Android.Content;
using System.Collections.Generic;
using System.Linq;
using Android.Media;
using System.IO;
using Android.Net;
//using Android.Util;

namespace FlickrLiveWallpaper
{
    [Service(Label = "@string/app_name", Permission = "android.permission.BIND_WALLPAPER")]
    [IntentFilter(new string[] { "android.service.wallpaper.WallpaperService" })]
    [MetaData("android.service.wallpaper", Resource = "@xml/wallpaper")]
    public class Wallpaper : WallpaperService
    {
        public class FeedDetail
        {
            public bool Include;
            public Func<int, Task<PhotoCollection>> func;
        }

        public override WallpaperService.Engine OnCreateEngine()
        {
            return new FlickrEngine(this);
        }

        public class FlickrEngine : WallpaperService.Engine
        {
            //private LockScreenVisibleReceiver mLockScreenVisibleReceiver;

            const float MIN_WIDTH_MULTIPLIER = 1.5f; // 1.5 - arbitrary minimun canvas width multiplier for scrolling
            private float xoffset;
            private int mWidth;
            private int mHeight;
            private int mNoOfPages = 3;
            private GestureDetector mGestDetect;
            private MySimpleOnGestureListener mGestList;

            public FlickrEngine(Wallpaper wall) : base(wall)
            {
                //mDrawFrame = delegate { DrawFrame(); };
            }

            public override void OnCreate(ISurfaceHolder surfaceHolder)
            {
                base.OnCreate(surfaceHolder);
                //mLockScreenVisibleReceiver = new LockScreenVisibleReceiver();
                //mLockScreenVisibleReceiver.setupRegisterDeregister(Application.Context);
                SetTouchEventsEnabled(true);
                SetOffsetNotificationsEnabled(true);
                mGestList = new MySimpleOnGestureListener();
                mGestList.DoubleTap = DoubleTap;
                mGestList.LongPress = LongPress;
                mGestDetect = new GestureDetector(Application.Context, mGestList);
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
            }

            // "It is very important that a wallpaper only use CPU while it is visible.. "
            public override void OnVisibilityChanged(bool visible)
            {
                //if (IsPreview)
                //    GbmpWallpaper = null;
                if (visible)
                    DrawFrame();
            }

            // "This method is always called at least once, after surfaceCreated(SurfaceHolder)."
            public override void OnSurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                base.OnSurfaceChanged(holder, format, width, height);
                mHeight = height;
                mWidth = width;
                DrawFrame();
            }

            /*
            // "After returning from this call, you should no longer try to access this surface."
            public override void OnSurfaceDestroyed(ISurfaceHolder holder)
            {
                base.OnSurfaceDestroyed(holder);
            }
            */

            private async Task<bool> UpdateTotals(bool loggedIn)
            {
                foreach (Feeds feed in Enum.GetValues(typeof(Feeds)))
                {
                    if (FlickrFeedsENabled(feed, loggedIn))
                    {
                        if (!mTotals.ContainsKey(feed) || (mTotals[feed] <= 0))
                        {
                            var dum = await FlickrFeeds(feed, 1, loggedIn);
                            mTotals[feed] = dum.Total;
                        }
                    }
                    else
                        mTotals[feed] = 0;
                }
                return true;
            }

            private void DisplayMessage(string txt)
            {
                if (IsPreview || (GbmpWallpaper == null) || Settings.DebugMessages)
                {
                    ISurfaceHolder holder = SurfaceHolder;
                    Canvas c = null;
                    try
                    {
                        c = holder.LockCanvas();
                        if (c != null)
                        {
                            var cover = IsPreview ? CoverBackground.All : CoverBackground.Text;
                            drawText(c, txt, 2, cover);
                        }
                    }
                    finally
                    {
                        if (c != null)
                            holder.UnlockCanvasAndPost(c);
                    }
                }
            }

            public static DateTime lastUpdate = new DateTime(0);
            public static Dictionary<Feeds, int> mTotals = new Dictionary<Feeds, int>();
            public enum Feeds { Search, Favourites, Contacts }
            //private const int SEARCH = 0;
            //private const int FAVS = 1;

            private async void SetBmp()
            {
                var txt = "";
                try
                {
                    GettingBitmap = true;

                    DisplayMessage("Loading images from Flickr...");

                    var loggedIn = await MyFlickr.Test();

                    txt = loggedIn.ToString()[0].ToString();

                    await UpdateTotals(loggedIn);

                    var totPages = mTotals.ToList().Sum(a => a.Value);

                    txt += " " + (!string.IsNullOrEmpty(Settings.Tags + Settings.Text)).ToString()[0] + " " + (loggedIn && Settings.Favourites).ToString()[0] + " " + (loggedIn && Settings.Contacts).ToString()[0] + " " + totPages;

                    string photoId = await GetRandomPhotoID(loggedIn);

                    /*
                    if (pc.Total == mIndex)
                        mIndex = 0;
                    */

                    DisplayMessage("Loading image sizes from Flickr...");

                    var size = await GetPhotoSize(photoId);

                    DisplayMessage("Downloading image from Flickr...");

                    GbmpWallpaper = await DownloadImage(photoId, size, txt);

                    DrawFrame();
                }
                catch (Exception ex)
                {
                    lastUpdate = new DateTime(0);
                    var bmpEr = GbmpWallpaper;
                    if (bmpEr == null)
                        bmpEr = Bitmap.CreateBitmap(2000, 2000, Bitmap.Config.Argb4444);
                    var c = new Canvas(bmpEr);
                    drawText(c, txt, 2.5f, CoverBackground.Text);
                    drawText(c, ex.Message + " " + DateTime.Now, 2, CoverBackground.Text);
                    GbmpWallpaper = bmpEr;
                }
                finally
                {
                    GettingBitmap = false;
                }
            }

    private async Task<PhotoCollection> FlickrFeeds(Feeds feed, int ipage, bool loggedIn)
            {
                var f = MyFlickr.getFlickr();
                switch (feed)
                {
                    case Feeds.Search:
                        var pso = new PhotoSearchOptions();
                        if (loggedIn)
                            switch (Settings.LimitUsers)
                            {
                                case "me": pso.UserId = "me"; break;
                                case "ff": pso.Contacts = ContactSearch.FriendsAndFamilyOnly; break;
                                case "contacts": pso.Contacts = ContactSearch.AllContacts; break;
                            }
                        pso.Tags = Settings.Tags;
                        pso.TagMode = Settings.AnyTag ? TagMode.AnyTag : TagMode.AllTags;
                        pso.Text = Settings.Text;
                        pso.PerPage = 1;
                        pso.Page = ipage;
                        return await f.PhotosSearchAsync(pso);

                    case Feeds.Favourites:
                        return await f.FavoritesGetListAsync(page: ipage, perPage: 1);

                    case Feeds.Contacts:
                        var ipc = await f.PhotosGetContactsPhotosAsync(ipage);
                        var opc = new PhotoCollection() { Total = ipc.Total };
                        opc.Add(ipc[ipage - 1]);
                        return opc;

                    default:
                        throw new Exception("Unknown Flickr Feed type.");
                }
            }

            private bool FlickrFeedsENabled(Feeds feed, bool loggedIn)
            {
                switch(feed)
                {
                    case Feeds.Search:
                        return !string.IsNullOrEmpty(Settings.Tags + Settings.Text);
                    case Feeds.Favourites:
                        return loggedIn && Settings.Favourites;
                    case Feeds.Contacts:
                        return loggedIn && Settings.Contacts;
                    default:
                        throw new Exception("Unknown Flickr Feed type.");
                }
            }

            private async Task<string> GetRandomPhotoID(bool loggedIn)
            {
                PhotoCollection pc = null;

                var totPages = mTotals.ToList().Sum(a => a.Value);

                if (totPages < 1)
                    throw new Exception("No images returned from Flickr feeds.");

                var page = new Random().Next(1, totPages);

                foreach (KeyValuePair<Feeds, int> tot in mTotals.ToList())
                {
                    if (page <= tot.Value)
                    {
                        pc = await FlickrFeeds(tot.Key, page, loggedIn);
                        mTotals[tot.Key] = pc.Total;
                        break;
                    }
                    else
                        page -= tot.Value;
                }
                if (pc == null)
                    throw new Exception("Can't find page " + page);

                if (pc.Count == 0)
                    throw new Exception("No photos returned.");

                return pc[0].PhotoId;
            }

            private async Task<Size> GetPhotoSize(string photoId)
            {
                var targetHeight = mHeight;
                var targetWidth = mWidth * (1 + mNoOfPages / 6);
                SizeCollection sc = await MyFlickr.getFlickr().PhotosGetSizesAsync(photoId);
                Size max = null;
                bool landscape = true;
                foreach (Size s in sc)
                {
                    var sHeight = s.Height;
                    var sWidth = s.Width;
                    if (s.Label != "Original")
                        landscape = s.Width > s.Height;
                    else if (landscape != (s.Width > s.Height))
                    {
                        sWidth = s.Height;
                        sHeight = s.Width;
                    }
                    // Always set if we haven't got a size already
                    if (max == null)
                        max = s;
                    // Set if this height/width is bigger than the one we have and the one we have is less than target
                    else if ((sHeight > max.Height) && (max.Height < targetHeight))
                        max = s;
                    else if ((sWidth > max.Width) && (max.Width < targetWidth))
                        max = s;
                    // Set if this height/width is bigger than target, but smaller than the one we have. ie, get the smallest picture that's big enough
                    else if ((sHeight > targetHeight) && (s.Height < max.Height))
                        max = s;
                    else if ((sWidth > targetWidth) && (s.Width < max.Width))
                        max = s;
                }

                return max;
            }

            private async Task<Bitmap> DownloadImage(string photoId, Size size, string debugMessage)
            {
                using (HttpClient hc = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, size.Source);
                    HttpResponseMessage response = await hc.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    BitmapFactory.Options opts = new BitmapFactory.Options();
                    opts.InMutable = true;
                    var bmp = await BitmapFactory.DecodeStreamAsync(await response.Content.ReadAsStreamAsync(), null, opts);

                    if (size.Label == "Original")
                    {
                        DisplayMessage("Getting image orientation from Flickr...");
                        var info = await MyFlickr.getFlickr().PhotosGetInfoAsync(photoId);
                        var orientation = info.Rotation;
                        debugMessage += " " + orientation;
                        if ((orientation > 0) && (orientation % 90 == 0))
                        {
                            var matrix = new Matrix();
                            matrix.PostRotate(orientation);
                            bmp = Bitmap.CreateBitmap(bmp, 0, 0, bmp.Width, bmp.Height, matrix, true);
                        }
                    }

                    debugMessage += " " + bmp.Width + "x" + bmp.Height;

                    if (Settings.DebugMessages)
                        drawText(new Canvas(bmp), debugMessage + " " + DateTime.Now, 2, CoverBackground.None);

                    if (!IsPreview)
                        lastUpdate = DateTime.Now;

                    return bmp;
                }
            }

            Bitmap GbmpWallpaper;
            private bool GettingBitmap = false;

            void DrawFrame()
            {
                var cm = (ConnectivityManager)Application.Context.GetSystemService(Android.Content.Context.ConnectivityService);
                NetworkInfo netInfo = cm.ActiveNetworkInfo;
                var connected = netInfo != null && netInfo.IsConnectedOrConnecting;

                if (!connected)
                    DisplayMessage("No internet connection.");
                else if (!GettingBitmap && (IsPreview || (lastUpdate.AddHours(Settings.IntervalHours) < DateTime.Now)))
                    SetBmp();
                // We can't set GettingBitmap here because we're deliberately not awaiting the SetBmp

                if (GbmpWallpaper != null)
                    UseCanvas(GbmpWallpaper);

            }

            /*
            private void UseManager(Bitmap bmpWallpaper)
            {
                Bitmap bmp = bmpWallpaper;
                var wallpaperManager = WallpaperManager.GetInstance(Application.Context);
                wallpaperManager.SetBitmap(bmp);
            }
            */

            public override void OnOffsetsChanged(float xOffset, float yOffset, float xOffsetStep, float yOffsetStep, int xPixelOffset, int yPixelOffset)
            {
                xoffset = xOffset;
                mNoOfPages = (xOffsetStep > 0) ? (int)Math.Round(1 + 1 / xOffsetStep) : 1;
                if (GbmpWallpaper != null)
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
                        string txt = "";

                        try
                        {
                            //const float MAX_RATIO = 2f;
                            var hRat = (float)c.Height / bmpWallpaper.Height;
                            var wRat = (1 + mNoOfPages / 6) * (c.Width / bmpWallpaper.Width);
                            var scale = Math.Max(wRat, hRat);
                            var width = (int)Math.Round(bmpWallpaper.Width * scale);
                            var height = (int)Math.Round(bmpWallpaper.Height * scale);
                            var hDiff = (c.Height - height) / 2;

                            //var wDiff = (c.Width - width) / 2;
                            //var left = (int) Math.Round((c.Width - width) * xoffset);

                            //var wDiff = (Math.Min(scale, 2) - 1) * c.Width; // 2 - arbitrary maximum canvas width multiplier for scrolling
                            //var left = (int)Math.Round(wDiff - (width / 2) - (wDiff * xoffset));

                            var wScale = Math.Min(0.5 + mNoOfPages / 2, (float)width / c.Width); // 2 - arbitrary maximum canvas width multiplier for scrolling
                            var wOffset = ((c.Width * wScale) - width) / 2;

                            var locOffset = IsPreview ? 0.5 : xoffset;

                            var left = (int)Math.Round(wOffset - locOffset * (wScale - 1) * c.Width);

                            // var top = (int)Math.Round((c.Height - height) * yoffset);

                            var src = new Rect(0, 0, bmpWallpaper.Width - 1, bmpWallpaper.Height - 1);
                            var dst = new Rect(left, hDiff, width + left, height + hDiff);

                            txt = xoffset + " " + left + " " + c.Width + " " + width + " " + DateTime.Now; // + " " + mLockScreenVisibleReceiver.LockScreenVisible;

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

                            if (Settings.DebugMessages)
                                drawText(c, txt, 3, CoverBackground.None);
                        }
                        catch (Exception ex)
                        {
                            drawText(c, txt + " " + System.Environment.NewLine + ex.Message, 3, CoverBackground.Text);
                        }
                    }
                }
                finally
                {
                    if (c != null)
                        holder.UnlockCanvasAndPost(c);
                }
            }

            private enum CoverBackground { None, Text, All }

            private void drawText(Canvas c, string txt, float div, CoverBackground coverBackground)
            {
                Paint p = new Paint();
                p.Alpha = 255;
                p.AntiAlias = true;
                p.TextSize = c.Height / 50;

                float w = p.MeasureText(txt, 0, txt.Length);
                int aoffset = (int)w / 2;
                int x = c.Width / 2 - aoffset;
                int y = (int)(c.Height / div);

                if (coverBackground != CoverBackground.None)
                {
                    p.Color = Color.Black;
                    if (coverBackground == CoverBackground.All)
                        c.DrawRect(0, 0, c.Width, c.Height, p);
                    else
                        c.DrawRect(0, y + p.TextSize / 4, c.Width, y - p.TextSize, p);
                }

                p.Color = Color.Yellow;
                c.DrawText(txt, x, y, p);
            }

            private DateTime mLastTap = DateTime.Now;
            public override Bundle OnCommand(string action, int x, int y, int z, Bundle extras, bool resultRequested)
            {
                if (action.StartsWith("android.wallpaper."))
                {
                    DisplayMessage("command " + (DateTime.Now - mLastTap).TotalMilliseconds.ToString());
                    mLastTap = DateTime.Now;
                }
                return base.OnCommand(action, x, y, z, extras, resultRequested);
            }

            private DateTime mLastTouch = DateTime.Now;
            public override void OnTouchEvent(MotionEvent e)
            {
                base.OnTouchEvent(e);
                //DisplayMessage(e.Source.ToString());
                mGestDetect.OnTouchEvent(e);
            }

            private void DoubleTap()
            {
                DisplayMessage("double " + DateTime.Now);
            }

            private void LongPress()
            {
                DisplayMessage("long " + DateTime.Now);
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

            public class MySimpleOnGestureListener : GestureDetector.SimpleOnGestureListener
            {
                public Action DoubleTap;
                public Action LongPress;

                public override bool OnDown(MotionEvent e)
                {
                    return base.OnDown(e);
                }

                public override bool OnDoubleTap(MotionEvent e)
                {
                    DoubleTap();
                    return base.OnDoubleTap(e);
                }

                public override void OnLongPress(MotionEvent e)
                {
                    LongPress();
                    base.OnLongPress(e);
                }
            }

        }



        /*
        public class BaseFlickrFeed
        {

        }

        public class FlickrSearchFeed : BaseFlickrFeed
        {
            public async Task<PhotoCollection> GetPhoto(int page)
            {
                PhotoSearchOptions pso = new PhotoSearchOptions();

                //if (loggedIn)
                switch (Settings.LimitUsers)
                {
                    case "me": pso.UserId = "me"; break;
                    case "ff": pso.Contacts = ContactSearch.FriendsAndFamilyOnly; break;
                    case "contacts": pso.Contacts = ContactSearch.AllContacts; break;
                }
                pso.Tags = Settings.Tags;
                pso.TagMode = Settings.AnyTag ? TagMode.AnyTag : TagMode.AllTags;
                pso.Text = Settings.Text;
                pso.PerPage = 1;

                pso.Page = page;
                return await MyFlickr.getFlickr().PhotosSearchAsync(pso);

            }
        }

        public class FlickrFavouritesFeed : BaseFlickrFeed
        {
            public async Task<PhotoCollection> GetPhoto(int page)
            {
                return await MyFlickr.getFlickr().FavoritesGetListAsync(page: page, perPage: 1);
            }
        }

        public class FlickrContactsFeed : BaseFlickrFeed
        {
            public async Task<PhotoCollection> GetPhoto(int page)
            {
                var ipc = await MyFlickr.getFlickr().PhotosGetContactsPhotosAsync(ipage);
                var opc = new PhotoCollection() { Total = ipc.Total };
                opc.Add(ipc[ipage - 1]);
                return opc;
            }
        } */
    }
}

