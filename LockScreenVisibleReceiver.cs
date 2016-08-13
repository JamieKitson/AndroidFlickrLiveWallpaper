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
using Android.Preferences;

namespace FlickrLiveWallpaper
{

    public class LockScreenVisibleChangedEvent
    {
        public bool mLockScreenVisible = false;

        public LockScreenVisibleChangedEvent(bool lockScreenVisible)
        {
            mLockScreenVisible = lockScreenVisible;
        }

        public bool isLockScreenVisible()
        {
            return mLockScreenVisible;
        }
    }
    /*
    public class anan : Preference, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        public override void OnSharedPreferenceChanged(ISharedPreferences sp, string key)
        {

        }
    }
    */
    class LockScreenVisibleReceiver : BroadcastReceiver // , ISharedPreferencesOnSharedPreferenceChangeListener
    {
        private bool mRegistered = false;
        private Context mRegisterDeregisterContext;
        public static String PREF_ENABLED = "settting";
        public bool LockScreenVisible = false;

        /*
        private ISharedPreferencesOnSharedPreferenceChangeListener mOnSharedPreferenceChangeListener
                = new {
                */
                /*
        public void OnSharedPreferenceChanged(ISharedPreferences sp, String key)
        {
            if (PREF_ENABLED.Equals(key))
            {
                registerDeregister(sp.GetBoolean(PREF_ENABLED, false));
            }
        }

        //*/

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent != null)
            {
                if (Intent.ActionUserPresent.Equals(intent.Action))
                {
                    // EventBus.GetDefault().post(new LockScreenVisibleChangedEvent(false));
                    LockScreenVisible = false;
                }
                else if (Intent.ActionScreenOff.Equals(intent.Action))
                {
                    // EventBus.getDefault().post(new LockScreenVisibleChangedEvent(true));
                    LockScreenVisible = true;
                }
                else if (Intent.ActionScreenOn.Equals(intent.Action))
                {
                    KeyguardManager kgm = (KeyguardManager)context.GetSystemService(Context.KeyguardService);
                    if (!kgm.InKeyguardRestrictedInputMode())
                    {
                        // EventBus.getDefault().post(new LockScreenVisibleChangedEvent(false));
                        LockScreenVisible = false;
                    }
                }
            }
        }

        private static IntentFilter createIntentFilter()
        {
            IntentFilter presentFilter = new IntentFilter();
            presentFilter.AddAction(Intent.ActionUserPresent);
            presentFilter.AddAction(Intent.ActionScreenOff);
            presentFilter.AddAction(Intent.ActionScreenOn);
            return presentFilter;
        }

        public void setupRegisterDeregister(Context context)
        {
            mRegisterDeregisterContext = context;
            registerDeregister(true);
            /*
            PreferenceManager.GetDefaultSharedPreferences(context)
                    .RegisterOnSharedPreferenceChangeListener(this); // mOnSharedPreferenceChangeListener);
            this.OnSharedPreferenceChanged(
                    PreferenceManager.GetDefaultSharedPreferences(context), PREF_ENABLED);
            */
        }

        private void registerDeregister(bool register)
        {
            if (mRegistered == register || mRegisterDeregisterContext == null)
            {
                return;
            }

            if (register)
            {
                mRegisterDeregisterContext.RegisterReceiver(this, createIntentFilter());
            }
            else
            {
                mRegisterDeregisterContext.UnregisterReceiver(this);
            }

            mRegistered = register;
        }

        public void Destroy()
        {
            registerDeregister(false);
            if (mRegisterDeregisterContext != null)
            {
                //PreferenceManager.GetDefaultSharedPreferences(mRegisterDeregisterContext)
                  //      .UnregisterOnSharedPreferenceChangeListener(this);
            }
        }
    }
}