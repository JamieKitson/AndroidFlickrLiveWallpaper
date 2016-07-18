using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Webkit;
using Android.Graphics;
using FlickrNet;
using Android.Preferences;

namespace FlickrLiveWallpaper
{
    [Activity(Label = "FlickrAuthActivity")]
    public class FlickrAuthActivity : Activity
    {
        public const string CALL_BACK = "http://kitten-x.com";

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            int i = 0;
            try
            {
                WebView webview = new WebView(Application.Context);
                i = 1;
                //Activity.view
                //        webview.Id = Android.Resource.Id.List;
                SetContentView(webview);
                i = 2;
                webview.Settings.JavaScriptEnabled = true;
                i = 3;
                i = 4;

                Flickr f = new Flickr(FlickrKeys.APIKey, FlickrKeys.SharedSecret);
                OAuthRequestToken rt = await f.OAuthRequestTokenAsync(CALL_BACK);
                string url = f.OAuthCalculateAuthorizationUrl(rt.Token, AuthLevel.Read);
                webview.SetWebViewClient(new TrapFlickrAuth(rt, this));
                webview.LoadUrl(url);
            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, i + " " + ex.Message, ToastLength.Short).Show();
            }
        }
    }

    public class TrapFlickrAuth : WebViewClient
    {
        const string OAUTH_VERIFIER = "oauth_verifier";
        const string OAUTH_TOKEN = "oauth_token";

        OAuthRequestToken requestToken;
        Activity mActivity;

        public TrapFlickrAuth(OAuthRequestToken rt, Activity act) : base()
        {
            requestToken = rt;
            mActivity = act;
        }

        public async override void OnPageStarted(WebView view, string url, Bitmap favicon)
        {
            // Toast.MakeText(Application.Context, url, ToastLength.Short).Show();
            base.OnPageStarted(view, url, favicon);

            Uri uri = new Uri(url);
            string q = uri.Query;


            if (url.StartsWith(FlickrAuthActivity.CALL_BACK) || (q.Contains(OAUTH_VERIFIER) && q.Contains(OAUTH_TOKEN)))
            {
                view.StopLoading();

                Toast.MakeText(Application.Context, "Found Auth URL", ToastLength.Short).Show();
                //e.Cancel = true;

                Dictionary<string, string> ps = new Dictionary<string, string>();
                foreach (string s in q.Substring(1).Split('&')) // substr(1) - don't want the leading question mark
                {
                    string[] p = s.Split('=');
                    if (p.Count() == 2)
                        ps.Add(p[0], p[1]);
                }
                if (ps.ContainsKey(OAUTH_VERIFIER))
                {
                    Toast.MakeText(Application.Context, "Found verifier", ToastLength.Short).Show();
                    Flickr f = new Flickr(FlickrKeys.APIKey, FlickrKeys.SharedSecret);
                    try
                    {
                        OAuthAccessToken tok = await f.OAuthAccessTokenAsync(requestToken.Token, requestToken.TokenSecret, ps[OAUTH_VERIFIER]);
                        if ((tok != null) && !string.IsNullOrEmpty(tok.Token) && !string.IsNullOrEmpty(tok.TokenSecret))
                        {
                            Toast.MakeText(Application.Context, tok.Token + "-" + tok.TokenSecret, ToastLength.Short).Show();
                            Settings.OAuthAccessToken = tok.Token;
                            Settings.OAuthAccessTokenSecret = tok.TokenSecret;
                            Toast.MakeText(Application.Context, "Success", ToastLength.Short).Show();
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                mActivity.Finish();
            }
                
        }
    }

}