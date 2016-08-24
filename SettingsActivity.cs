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
using static FlickrLiveWallpaper.Wallpaper;
using static FlickrLiveWallpaper.Wallpaper.FlickrEngine;
using Android.Runtime;
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

                    var p = PreferenceScreen.FindPreference("flickr_auth");
                    p.OnPreferenceClickListener = this;
                    UpdateLoggedIn();

                    SetChangeListener(Settings.INTERVAL, updateIntervalSummary, Settings.IntervalHours);
                    SetChangeListener(Settings.DEBUG_MESSAGES);
                    SetChangeListener(Settings.TAGS, updateTagSummary, Settings.Tags);
                    SetChangeListener(Settings.ANY_TAG);
                    SetChangeListener(Settings.TEXT, updateTextSummary, Settings.Text);
                    SetChangeListener(Settings.LIMIT_USERS, updateLimitUsersSummary, Settings.LimitUsers);
                    SetChangeListener(Settings.FAVOURITES);
                    SetChangeListener(Settings.CONTACTS);
                }
                catch (Exception ex)
                {
                    Toast.MakeText(Application.Context, "Error: " + ex.Message, ToastLength.Long).Show();
                }
            }

            private Preference SetChangeListener(string name)
            {
                Preference p = PreferenceScreen.FindPreference(name);
                p.OnPreferenceChangeListener = this;
                return p;
            }

            private void SetChangeListener<T>(string name, Action<Preference, T> update, T value)
            {
                Preference p = SetChangeListener(name);
                update(p, value);
            }

            public bool OnPreferenceChange(Preference preference, Java.Lang.Object newValue)
            {
                Settings.ClearCache();

                if (preference.Key == Settings.INTERVAL)
                {
                    string si = newValue.ToString();
                    float ii;
                    if (float.TryParse(si, out ii))
                    {
                        updateIntervalSummary(preference, ii);
                        return true;
                    }
                    else
                        return false;
                }

                if (preference.Key == Settings.TAGS)
                    updateTagSummary(preference, newValue.ToString());

                if (preference.Key == Settings.TEXT)
                    updateTextSummary(preference, newValue.ToString());

                if (preference.Key == Settings.LIMIT_USERS)
                    updateLimitUsersSummary(preference, newValue.ToString());

                mTotals.Clear();
                lastUpdate = new DateTime(0);

                return true;

                /*
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
                else if (preference.Key.ToLower() == Settings.DEBUG_MESSAGES)
                {
                    //Settings.ClearCache(); // DebugMessages = bool.Parse(newValue.ToString());
                    return true;
                }

                switch(preference.Key)
                {
                    case Settings.TAGS:
                    case Settings.TEXT:
                    case Settings.ANY_TAG:
                    case Settings.LIMIT_USERS:
                        return true;
                }
                return false;
                */
            }

            private void updateIntervalSummary(Preference pref, float ii)
            {
                pref.Summary = "Currently: " + ii + " hours.";
            }

            private void updateTagSummary(Preference pref, string s)
            {
                pref.Summary = "Comma separated. Currently: " + s;
            }

            private void updateTextSummary(Preference pref, string s)
            {
                pref.Summary = "Currently: " + s;
            }

            private void updateLimitUsersSummary(Preference pref, string s)
            {
                if (s == "ff")
                    s = "Friends and Family";
                else
                    s = s[0].ToString().ToUpper() + s.Substring(1); 
                pref.Summary = "Currently: " + s;
            }

            public bool OnPreferenceClick(Preference preference)
            {
                test();
                return true;
            }

            private /*async*/ void test()
            {
                /*
                var prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                var tok = prefs.GetString("token", "");
                var secret = prefs.GetString("secret", "");
                */
                /*
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
                */

                Settings.UnsetTokens();

                // if (!loggedin)

                StartActivityForResult(new Intent(Application.Context, typeof(FlickrAuthActivity)), 2);

                UpdateLoggedIn();

                mTotals.Clear();
                lastUpdate = new DateTime(0);

            }

            public override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
            {
                base.OnActivityResult(requestCode, resultCode, data);
                if ((requestCode == 2) && (resultCode == Result.Ok))
                {
                    UpdateLoggedIn();
                }
            }

            private async void UpdateLoggedIn()
            {
                var p = PreferenceScreen.FindPreference("flickr_auth");

                if (Settings.TokensSet())
                {
                    Flickr f = MyFlickr.getFlickr();
                    var fu = await f.TestLoginAsync(); // .ContinueWith(a =>
                    //{
                        p.Summary = "Logged in as: " + fu.UserName;
                    //});
                }
                else
                    p.Summary = "Not logged in.";

            }
        }
        /*
        public class TrapFlickrAuth : WebViewClient
        {
            public override void OnPageStarted(WebView view, string url, Bitmap favicon)
            {
                Toast.MakeText(Application.Context, url, ToastLength.Short).Show();
                base.OnPageStarted(view, url, favicon);
            }
        }
        */
    }
}