using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
//using Android.Runtime;
//using Android.Views;
using Android.Widget;
using Android.Preferences;
using Android.Graphics;
using Android.Util;
using Android.Webkit;
using FlickrNet;
using System.Threading.Tasks;
// using Java.Lang;

namespace FlickrLiveWallpaper
{
    [Activity(Label = "SettingsActivity", Name = "xyz.kitson.jamie.flickrlivewallpaper.prefs", Exported = true)]
    //[IntentFilter(new[] { Intent.ActionMain }, Categories = new[] { Intent.CategoryLauncher })]
    public class SettingsActivity : PreferenceActivity // , Preference.IOnPreferenceChangeListener
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {

                base.OnCreate(savedInstanceState);
                SettingsFragment sa = new SettingsFragment();
                FragmentManager.BeginTransaction().Replace(Android.Resource.Id.Content, sa).Commit();                

            }
            catch (Exception ex)
            {
                Log.Error("", ex.Message);
            }
        }

        public class SettingsFragment : PreferenceFragment, Preference.IOnPreferenceChangeListener, Preference.IOnPreferenceClickListener
        {
            public override void OnCreate(Bundle savedInstanceState)
            {
                try
                {
                    base.OnCreate(savedInstanceState);
                    AddPreferencesFromResource(Resource.Xml.prefs);

                    Preference p = PreferenceScreen.FindPreference(Settings.INTERVAL);
                    p.OnPreferenceChangeListener = this;
                    updateIntervalSummary(p, Settings.IntervalHours);

                    p = PreferenceScreen.FindPreference("flickr_auth");
                    p.OnPreferenceClickListener = this;

                    p = PreferenceScreen.FindPreference(Settings.USE_WALLPAPER);
                    p.OnPreferenceChangeListener = this;

                    p = PreferenceScreen.FindPreference(Settings.DEBUG_MESSAGES);
                    p.OnPreferenceChangeListener = this;
                }
                catch (Exception ex)
                {
                    Toast.MakeText(Application.Context, "Error: " + ex.Message, ToastLength.Long).Show();
                }
            }

            public bool OnPreferenceChange(Preference preference, Java.Lang.Object newValue)
            {
                Settings.ClearCache();
                if (preference.Key.ToLower() == Settings.INTERVAL)
                {
                    string si = newValue.ToString();
                    float ii;
                    if (float.TryParse(si, out ii))
                    {
                        //Settings.IntervalHours = ii;
                        updateIntervalSummary(preference, ii);
                        return true;
                    }
                    else
                        return false;
                }
                else if (preference.Key.ToLower() == Settings.USE_WALLPAPER)
                {
                    //Settings.UseWallpaper = bool.Parse(newValue.ToString());
                    return true;
                }
                else if (preference.Key.ToLower() == Settings.DEBUG_MESSAGES)
                {
                    //Settings.ClearCache(); // DebugMessages = bool.Parse(newValue.ToString());
                    return true;
                }
                return false;


            }

            private void updateIntervalSummary(Preference pref, float ii)
            {
                pref.Summary = "Currently: " + ii + " hours.";
            }

            public bool OnPreferenceClick(Preference preference)
            {
                test();
                return true;
            }

            private async void test()
            {
                /*
                var prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                var tok = prefs.GetString("token", "");
                var secret = prefs.GetString("secret", "");
                */
                Toast.MakeText(Application.Context, Settings.OAuthAccessToken + "-" + Settings.OAuthAccessTokenSecret, ToastLength.Short).Show();
                var loggedin = false;

                if (Settings.TokensSet())
                {
                    Toast.MakeText(Application.Context, "2", ToastLength.Short).Show();
                    Flickr f = MyFlickr.getFlickr();

                    try
                    {
                        Toast.MakeText(Application.Context, "3", ToastLength.Short).Show();
                        FoundUser fu = await f.TestLoginAsync();
                        Toast.MakeText(Application.Context, "4", ToastLength.Short).Show();

                        Toast.MakeText(Application.Context, fu.UserId, ToastLength.Short).Show();
                        loggedin = true;
                    }
                    catch
                    {
                        //
                    }

                }

                if (!loggedin)
                    StartActivity(new Intent(Application.Context, typeof(FlickrAuthActivity)));

            }
        }

        public class TrapFlickrAuth : WebViewClient
        {
            public override void OnPageStarted(WebView view, string url, Bitmap favicon)
            {
                Toast.MakeText(Application.Context, url, ToastLength.Short).Show();
                base.OnPageStarted(view, url, favicon);
            }
        }
    }
}