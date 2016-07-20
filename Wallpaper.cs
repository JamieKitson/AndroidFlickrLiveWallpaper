/*
 * Copyright (C) 2009 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Service.Wallpaper;
using Android.Views;
using FlickrNet;
using Java.Net;
using System.Net.Http;
using Android.Renderscripts;
using System.Threading.Tasks;
using Android.Widget;
using Android.Content.Res;
//using Android.Util;

namespace FlickrLiveWallpaper
{
	[Service (Label = "@string/app_name", Permission = "android.permission.BIND_WALLPAPER")]
	[IntentFilter (new string[] { "android.service.wallpaper.WallpaperService" })]
	[MetaData ("android.service.wallpaper", Resource = "@xml/wallpaper")]
	class Wallpaper : WallpaperService
	{
		public override WallpaperService.Engine OnCreateEngine ()
		{
			return new FlickrEngine (this);
		}

		class FlickrEngine : WallpaperService.Engine
		{
			private Handler mHandler = new Handler ();

			private Paint paint = new Paint ();
			// private PointF center = new PointF ();
			// private PointF touch_point = new PointF (-1, -1);
			// private float offset;
			//private long start_time;

            private Action mDrawFrame;
            //private Action mDownloadImage;
            // private bool is_visible;

            private int mIndex = new Random().Next(100);

			public FlickrEngine (Wallpaper wall) : base (wall)
			{
				// Set up the paint to draw the lines for our cube
				paint.Color = Color.Yellow;
				paint.AntiAlias = true;
				paint.StrokeWidth = 2;
				paint.StrokeCap = Paint.Cap.Round;
				paint.SetStyle (Paint.Style.Stroke);

				//start_time = SystemClock.ElapsedRealtime ();
				
				mDrawFrame = delegate { DrawFrame (); };
                //mDownloadImage = async delegate { await GetBmp(); };
			}

			public override void OnCreate (ISurfaceHolder surfaceHolder)
			{
				base.OnCreate (surfaceHolder);
                DrawFrame();
			}

			public override void OnDestroy ()
			{
				base.OnDestroy ();

				mHandler.RemoveCallbacks (mDrawFrame);
			}
			/*
			public override void OnVisibilityChanged (bool visible)
			{
				is_visible = visible;

				//if (visible)
					//DrawFrame();
				//else
					//mHandler.RemoveCallbacks (mDrawCube);
			}
            /*
			public override void OnSurfaceChanged (ISurfaceHolder holder, Format format, int width, int height)
			{
				base.OnSurfaceChanged (holder, format, width, height);

				// store the center of the surface, so we can draw the cube in the right spot
				center.Set (width / 2.0f, height / 2.0f);

                // store the center of the surface, so we can draw the cube in the right spot
                center.Set(width / 2.0f, height / 2.0f);

                DrawFrame();
            }
		    /*
			public override void OnSurfaceDestroyed (ISurfaceHolder holder)
			{
				base.OnSurfaceDestroyed (holder);

				is_visible = false;
				//mHandler.RemoveCallbacks (mDownloadImage);
			}
            /*
			public override void OnOffsetsChanged (float xOffset, float yOffset, float xOffsetStep, float yOffsetStep, int xPixelOffset, int yPixelOffset)
			{
				offset = xOffset;

				DrawFrame ();
			}
            */
            private Bitmap bmpWallpaper;
            private DateTime lastUpdate;
            // private Rect rectDest;

            async Task<Bitmap> GetBmp()
            {

                Flickr f = MyFlickr.getFlickr();
                PhotoSearchOptions pso = new PhotoSearchOptions("77788903@N00", "myfavs");
                // pso.Extras = PhotoSearchExtras.OriginalFormat;
                pso.PerPage = 1;
                pso.Page = mIndex++;
                PhotoCollection pc = await f.PhotosSearchAsync(pso);

                if (pc.Total == mIndex)
                    mIndex = 0;

                // int i = mIndex++;

                SizeCollection sc = await f.PhotosGetSizesAsync(pc[0].PhotoId);
                Size max = null;
                foreach (Size s in sc)
                {

                    if ((max == null) || (s.Height > max.Height))
                        max = s;
                }

                string url = max.Source; // pc[0].LargeUrl; // pc[i].LargeUrl;

                // await new System.Threading.Tasks.Task<Bitmap>() {
                using (HttpClient hc = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage response = await hc.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    lastUpdate = DateTime.Now;
                    return BitmapFactory.DecodeStream(await response.Content.ReadAsStreamAsync());
                    // return bmpWallpaper;
                    //c.DrawBitmap(bmp, 0, 0, p);
                    /*
                    var hRat = (float)c.Height / bmpWallpaper.Height;
                    var wRat = (float)c.Width / bmpWallpaper.Width;
                    var scale = Math.Max(wRat, hRat);
                    var width = (int)Math.Round(bmpWallpaper.Width * scale);
                    var height = (int)Math.Round(bmpWallpaper.Height * scale);
                    var hDiff = (c.Height - height) / 2;
                    var wDiff = (c.Width - width) / 2;

                    //var src = new Rect(0, 0, bmpWallpaper.Width - 1, bmpWallpaper.Height - 1);
                    rectDest = new Rect(wDiff, hDiff, width + wDiff, height + hDiff);
                    */
                }

                //mHandler.RemoveCallbacks (mDownloadImage);

                // if (is_visible)
                //	mHandler.PostDelayed (mDownloadImage, (long)(lastUpdate.AddHours(Settings.IntervalHours) - DateTime.Now).TotalMilliseconds);


            }


            // Draw one frame of the animation. This method gets called repeatedly
            // by posting a delayed Runnable. You can do any drawing you want in
            // here. This example draws a wireframe cube.
            async void DrawFrame ()
			{

                ISurfaceHolder holder = SurfaceHolder;

				Canvas c = null;

				try {
					c = holder.LockCanvas ();

					if (c != null) {

                        string txt = "-";

                        Paint p = new Paint();
                        p.Alpha = 255;
                        p.AntiAlias = true;

                        p.Color = Color.Black;

                        c.DrawRect(0, 0, c.Width, c.Height, p);

                        p.Color = Color.Yellow;
                        p.TextSize = 30;

                        try
                        {

                            if ((bmpWallpaper == null) || (lastUpdate == null) || (lastUpdate.AddHours(Settings.IntervalHours) < DateTime.Now))
                            {
                                bmpWallpaper = await GetBmp();
                                // bmpWallpaper = await GetBmp();
                                //lastUpdate = DateTime.Now;
                                //}

                                //Android.Util.Log.Info("", mIndex.ToString());

                                /*
                                 * 
                                Flickr f = MyFlickr.getFlickr();
                                PhotoSearchOptions pso = new PhotoSearchOptions("77788903@N00", "myfavs");
                                // pso.Extras = PhotoSearchExtras.OriginalFormat;
                                pso.PerPage = 1;
                                pso.Page = mIndex++;
                                PhotoCollection pc = await f.PhotosSearchAsync(pso);

                                if (pc.Total == mIndex)
                                    mIndex = 0;

                                // int i = mIndex++;

                                SizeCollection sc = await f.PhotosGetSizesAsync(pc[0].PhotoId);
                                Size max = null;
                                foreach (Size s in sc)
                                {

                                    if ((max == null) || (s.Height > max.Height))
                                        max = s;
                                }

                                string url = max.Source; // pc[0].LargeUrl; // pc[i].LargeUrl;

                                txt = " " + pc[0].Title + " " + max.Label + " " + max.Width + "x" + max.Height; // + url;

                                // await new System.Threading.Tasks.Task<Bitmap>() {
                                using (HttpClient hc = new HttpClient())
                                {
                                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                                    HttpResponseMessage response = await hc.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                                    bmpWallpaper = BitmapFactory.DecodeStream(await response.Content.ReadAsStreamAsync());

                                    //c.DrawBitmap(bmp, 0, 0, p);
                                }
                                */
                            }

                            var hRat = (float)c.Height / bmpWallpaper.Height;
                            var wRat = (float)c.Width / bmpWallpaper.Width;
                            var scale = Math.Max(wRat, hRat);
                            var width = (int)Math.Round(bmpWallpaper.Width * scale);
                            var height = (int)Math.Round(bmpWallpaper.Height * scale);
                            var hDiff = (c.Height - height) / 2;
                            var wDiff = (c.Width - width) / 2;

                            var src = new Rect(0, 0, bmpWallpaper.Width - 1, bmpWallpaper.Height - 1);
                            var dst = new Rect(wDiff, hDiff, width + wDiff, height + hDiff);

                            // c.DrawBitmap(bmp, src, dst, null);
                            /*
                            txt = // System.Environment.NewLine +
                                Resources.System.DisplayMetrics.HeightPixels + " " +
                                Resources.System.DisplayMetrics.WidthPixels + " " +
                                hRat + " " +
                                wRat + " " +
                                scale + " " +
                                width + " " +
                                height + " " +
                                hDiff + " " +
                                wDiff + " " +
                                (width + wDiff) + " " +
                                (height + hDiff);
                                */
                            //*

                            txt = lastUpdate + " " + lastUpdate.AddHours(Settings.IntervalHours) + " " + DateTime.Now + " " + (lastUpdate.AddHours(Settings.IntervalHours) - DateTime.Now).TotalMilliseconds;

                            // bmp.Height

                            await Task.Factory.StartNew(() =>
                            {
                                return CreateBlurredImage(bmpWallpaper.Copy(bmpWallpaper.GetConfig(), false), 10);
                            }).ContinueWith(task =>
                            {
                                Bitmap res = task.Result; // .Copy(Bitmap.Config.Argb8888, true);
                                    c.DrawBitmap(res, src, dst, null);
                                    //c.DrawBitmap(res, 0, 0, p);
                                }, TaskScheduler.FromCurrentSynchronizationContext());
                            // */
                            // }
                        }
                        catch (Exception ex)
                        {
                            txt += System.Environment.NewLine + ex.Message;
                        }

                        float w = p.MeasureText(txt, 0, txt.Length);
                        int offset = (int)w / 2;
                        int x = c.Width / 2 - offset;
                        int y = c.Height / 2;

                        c.DrawText(txt, x, y, p);

                    }
                } finally {
					if (c != null)
						holder.UnlockCanvasAndPost (c);
				}

				// Reschedule the next redraw
				mHandler.RemoveCallbacks (mDrawFrame);

				// if (is_visible)
					mHandler.PostDelayed (mDrawFrame, (long)(lastUpdate.AddHours(Settings.IntervalHours) - DateTime.Now).TotalMilliseconds);
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
