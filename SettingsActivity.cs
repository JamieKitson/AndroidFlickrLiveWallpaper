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
    [Activity(Label = "SettingsActivity", Name = "xyz.kitson.jamie.wallpaper.prefs", Exported = true)]
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
                base.OnCreate(savedInstanceState);
                AddPreferencesFromResource(Resource.Xml.prefs);

                Preference p = PreferenceScreen.FindPreference("interval");
                p.OnPreferenceChangeListener = this;
                updateIntervalSummary(p, Settings.Interval);

                p = PreferenceScreen.FindPreference("text_color");
                p.OnPreferenceChangeListener = this;

                p = PreferenceScreen.FindPreference("text_to_display");
                p.OnPreferenceChangeListener = this;

                // Toast.MakeText(Application.Context, "Looking for Button 1", ToastLength.Short).Show();
                p = PreferenceScreen.FindPreference("flickr_auth");
                if (p != null)
                {
                    Toast.MakeText(Application.Context, "Found Button 1", ToastLength.Short).Show();
                    p.OnPreferenceClickListener = this;
                }
            }

            /*
        }

        public class demo : Preference.IOnPreferenceChangeListener
        { */

            public bool OnPreferenceChange(Preference preference, Java.Lang.Object newValue)
            {
                if (preference.Key.ToLower() == "interval")
                {
                    string si = newValue.ToString();
                    float ii;
                    if (float.TryParse(si, out ii))
                    {
                        //Settings.Interval = ii;
                        updateIntervalSummary(preference, ii);
                        return true;
                    }
                    else
                        return false;
                }
                return false;

                /*
                if (preference.Key.ToLower() == "text_color")
                {
                    try
                    {

                        String input = newValue.ToString();

                        if (input.Length != 7)
                            throw new Exception("Invalid length");

                        if (!input.StartsWith("#"))
                            throw new Exception("Invalid format");

                        String r = input.Substring(1, 2);
                        String g = input.Substring(3, 2);
                        String b = input.Substring(5, 2);
                        Color.Rgb(int.Parse(r), int.Parse(g), int.Parse(b));
                        return true;

                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(Application.Context, ex.Message + "Invalid hex color value (example input: #ff0000).", ToastLength.Short).Show();
                        return false;
                    }
                }
                else if (preference.Key.ToLower() == "text_to_display")
                {
                    try
                    {
                        String input = newValue.ToString();
                        if (input.Length < 1)
                            throw new Exception("Invalid length");
                        return true;
                    }
                    catch (Exception) // e)
                    {
                        Toast.MakeText(Application.Context, "Invalid display string.", ToastLength.Short).Show();
                        return false;
                    }
                }
                return true;
                */
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