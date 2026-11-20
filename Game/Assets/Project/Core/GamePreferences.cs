using UnityEngine;

namespace TemporalContract.Core
{
    public static class GamePreferences
    {
        public static float Volume {get=>PlayerPrefs.GetFloat("tc.volume",.8f);set{PlayerPrefs.SetFloat("tc.volume",Mathf.Clamp01(value));AudioListener.volume=Volume;}}
        public static float Sensitivity {get=>PlayerPrefs.GetFloat("tc.sensitivity",.12f);set=>PlayerPrefs.SetFloat("tc.sensitivity",Mathf.Clamp(value,.03f,.3f));}
        public static float FieldOfView {get=>PlayerPrefs.GetFloat("tc.fov",70f);set=>PlayerPrefs.SetFloat("tc.fov",Mathf.Clamp(value,55,95));}
        public static void Save()=>PlayerPrefs.Save();
    }
}
