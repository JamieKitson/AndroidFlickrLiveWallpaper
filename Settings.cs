using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Util;
using Android.Widget;
using FlickrNet;
using Javax.Crypto;
using Javax.Crypto.Spec;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
//using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static Android.Provider.Settings;

namespace FlickrLiveWallpaper
{
    public class Settings
    {
        private static Dictionary<string, object> cache = new Dictionary<string, object>();

        public static void ClearCache()
        {
            cache.Clear();
        }

        private static T GetSetting<T>(string name, T defVal, Func<ISharedPreferences, T> getter)
        {
            var val = defVal;
            try
            {
                if (cache.ContainsKey(name))
                    return (T)cache[name];
                var prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                val = getter(prefs);
                cache[name] = val;
            }
            catch (Exception ex)
            {
                throw ex;
                //DebugLog("Error getting setting " + name + " : " + ex.Message);
            }
            return val;
        }

        private static string GetString(string name, string defVal)
        {
            return GetSetting(name, defVal, (prefs) => { return prefs.GetString(name, defVal); } );
        }

        private static bool GetBool(string name, bool defVal)
        {
            return GetSetting(name, defVal, (prefs) => { return prefs.GetBoolean(name, defVal); });
        }

        private static void SetString(string name, string val)
        {            
            var editor = PreferenceManager.GetDefaultSharedPreferences(Application.Context).Edit();
            editor.PutString(name, val);
            editor.Apply();
            cache[name] = val;
        }

        private const string TOKEN = "token";
        public static string OAuthAccessToken
        {
            get { return GetString(TOKEN, ""); }
            set { SetString(TOKEN, value); }
        }

        private const string SECRET = "secret";
        public static string OAuthAccessTokenSecret
        {
            get
            {

                var encoded = GetString(SECRET, null);
                if (!string.IsNullOrEmpty(encoded))
                {
                    var bin = Base64.Decode(encoded, Base64Flags.NoWrap);
                    Cipher pbeCipher = getCipher(bin, CipherMode.DecryptMode);
                    return Encoding.UTF8.GetString(pbeCipher.DoFinal(bin));
                }
                return "";
            }
            set
            {
                byte[] bytes = value != null ? Encoding.UTF8.GetBytes(value) : new byte[0];
                Cipher pbeCipher = getCipher(bytes, CipherMode.EncryptMode);
                var encoded = Base64.EncodeToString(pbeCipher.DoFinal(bytes), Base64Flags.NoWrap);
                SetString(SECRET, encoded);
            }
        }

        private static Cipher getCipher(byte[] bytes, CipherMode mode)
        {
            SecretKeyFactory keyFactory = SecretKeyFactory.GetInstance("PBEWithMD5AndDES");
            ISecretKey key = keyFactory.GenerateSecret(new PBEKeySpec(FlickrKeys.CipherKey.ToCharArray()));
            Cipher pbeCipher = Cipher.GetInstance("PBEWithMD5AndDES");
            pbeCipher.Init(mode, key, new PBEParameterSpec(Encoding.UTF8.GetBytes(Secure.GetString(Application.Context.ContentResolver, Secure.AndroidId)), 20));
            return pbeCipher;
        }

        public static bool TokensSet()
        {
            return !string.IsNullOrEmpty(OAuthAccessToken + OAuthAccessTokenSecret);
        }

        public static void UnsetTokens()
        {
            OAuthAccessToken = "";
            OAuthAccessTokenSecret = "";
        }

        public const string INTERVAL = "interval";
        public static float IntervalHours
        {
            // The settings activity page stores this as a string
            get { return float.Parse(GetString(INTERVAL, "3")); }
        }

        public const string DEBUG_MESSAGES = "debug_messages";
        public static bool DebugMessages
        {
            get { return GetBool(DEBUG_MESSAGES, true); }
        }

        public const string LIMIT_USERS = "limit_users";
        public static string LimitUsers
        {
            get { return GetString(LIMIT_USERS, ""); }
        }

        public const string TAGS = "tags";
        public static string Tags
        {
            get { return GetString(TAGS, ""); }
        }

        public const string ANY_TAG = "any_tag";
        public static bool AnyTag
        {
            get { return GetBool(ANY_TAG, true); }
        }

        public const string TEXT = "text";
        public static string Text
        {
            get { return GetString(TEXT, ""); }
        }

        public const string FAVOURITES = "favourites";
        public static bool Favourites
        {
            get { return GetBool(FAVOURITES, true); }
        }

        public const string CONTACTS = "contacts";
        public static bool Contacts
        {
            get { return GetBool(CONTACTS, true); }
        }

        /*
        private static void DoLog(string msg, int level)
        {
            LogLine(DateTime.Now.ToString("s") + " " + level + " " + msg);
            if (level <= LogLevel)
                ToastMessage(msg);
        }

        public static void ErrorLog(string msg)
        {
            DoLog(msg, 0);
        }

        public static void LogInfo(string msg)
        {
            DoLog(msg, 1);
        }

        public static void DebugLog(string msg)
        {
            DoLog(msg, 2);
        }

        private static void ToastMessage(string msg)
        {
            Toast.MakeText(Application.Context, msg, ToastLength.Short).Show();
        }

        public enum ePrivacy { Private, Friends, Family, FriendsFamily, Public };
        const string PRIVACY = "privacy";
        public static ePrivacy Privacy
        {
            get { return GetSetting(PRIVACY, ePrivacy.Private); }
            set { SetSetting(PRIVACY, value); }
        }

        // I don't know if this is worth caching, but it will be called every time that Tags is called.
        private static string _PhoneModelName;
        public static string PhoneModelName
        {
            get
            {
                if (string.IsNullOrEmpty(_PhoneModelName))
                {
                    var man = Build.Manufacturer;
                    var mod = Build.Model;
                    if (mod.StartsWith(man))
                        _PhoneModelName = mod;
                    else
                        _PhoneModelName = man;
                }
                return _PhoneModelName;
            }
        }

        private const string TAGS = "tags";
        public static string Tags
        {
            get { return GetSetting(TAGS, "wpautouploader," + PhoneModelName); }
            set { SetSetting(TAGS, value); }
        }

        private const string TESTS_FAILED = "testsfailed";
        public static int TestsFailed
        {
            get { return GetSetting(TESTS_FAILED, 0); }
            set { SetSetting(TESTS_FAILED, value); }
        }

        private const string LOG_LEVEL = "loglevel";
        public static double LogLevel
        {
            get { return GetSetting<double>(LOG_LEVEL, 0); }
            set { SetSetting(LOG_LEVEL, value); }
        }

        private const string LOG = "log";

        private static void LogLine(string msg)
        {
            Mutex mutexFile = new Mutex(false, LOG);
            mutexFile.WaitOne();
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                using (var file = store.OpenFile(LOG, FileMode.Append))
                using (StreamWriter writer = new StreamWriter(file))
                    writer.WriteLine(msg);
            }
            finally
            {
                mutexFile.ReleaseMutex();
            }
        }

        public static void ClearLog()
        {
            Mutex mutexFile = new Mutex(false, LOG);
            mutexFile.WaitOne();
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string log;
                    using (var file = store.OpenFile(LOG, FileMode.OpenOrCreate))
                    using (var reader = new System.IO.StreamReader(file))
                        log = reader.ReadToEnd();

                    List<string> lines = log.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    bool removed = false;
                    while (lines.Count > 0)
                    {
                        string[] ss = lines[0].Split(new char[] { ' ' }, 2);
                        if (ss.Count() == 2)
                        {
                            DateTime dt;
                            if (DateTime.TryParse(ss[0], out dt) && (DateTime.Now - dt < new TimeSpan(24, 0, 0)))
                                break;
                        }
                        lines.RemoveAt(0);
                        removed = true;
                    }
                    if (removed)
                    {
                        using (var file = store.OpenFile(LOG, FileMode.OpenOrCreate))
                        {
                            file.SetLength(0);
                            using (StreamWriter writer = new StreamWriter(file))
                                writer.Write(string.Join(System.Environment.NewLine, lines));
                        }
                    }
                }
            }
            finally
            {
                mutexFile.ReleaseMutex();
            }
        }

        public static string GetLog()
        {
            Mutex mutexFile = new Mutex(false, LOG);
            mutexFile.WaitOne();
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                using (var file = store.OpenFile(LOG, FileMode.OpenOrCreate))
                using (var reader = new System.IO.StreamReader(file))
                    return reader.ReadToEnd();
            }
            finally
            {
                mutexFile.ReleaseMutex();
            }
        }

        private const string FLICKR_ALBUM = "flickralbum";
        public static Photoset FlickrAlbum
        {
            get { return GetSetting<Photoset>(FLICKR_ALBUM, null); }
            set { SetSetting(FLICKR_ALBUM, value); }
        }

        private const string UPLOADS_FAILED = "uploadsfailed";
        public static int UploadsFailed
        {
            get { return GetSetting(UPLOADS_FAILED, 0); }
            set { SetSetting(UPLOADS_FAILED, value); }
        }

        private const string UPLOAD_VIDEOS = "uploadvideos";
        public static bool UploadVideos
        {
            get { return GetSetting(UPLOAD_VIDEOS, true); }
            set { SetSetting(UPLOAD_VIDEOS, value); }
        }

        private const string UPLOAD_HI_RES = "uploadhires";
        public static bool UploadHiRes
        {
            get { return GetSetting(UPLOAD_HI_RES, true); }
            set { SetSetting(UPLOAD_HI_RES, value); }
        }

        private const string LAST_SUCCESSFUL_RUN = "lastsuccessfulrun";
        public static DateTime LastSuccessfulRun
        {
            get { return GetSetting(LAST_SUCCESSFUL_RUN, new DateTime(0)); }
            set { SetSetting(LAST_SUCCESSFUL_RUN, value); }
        }
        */
    }
}
