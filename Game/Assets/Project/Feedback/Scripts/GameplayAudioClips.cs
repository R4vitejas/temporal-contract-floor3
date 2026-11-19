using UnityEngine;

namespace TemporalContract.Feedback
{
    /// <summary>Original synthesized UI, mechanical and foot-contact sounds; no external audio assets.</summary>
    public static class GameplayAudioClips
    {
        static AudioClip step,reload,click;
        public static AudioClip Footstep=>step?step:(step=Create("Foot contact",.16f,0));
        public static AudioClip Reload=>reload?reload:(reload=Create("Magazine action",.42f,1));
        public static AudioClip Click=>click?click:(click=Create("Interface tick",.045f,2));
        static AudioClip Create(string name,float seconds,int kind)
        {
            const int rate=22050;var data=new float[Mathf.CeilToInt(rate*seconds)];var random=new System.Random(307+kind);float low=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate,n=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,n,.12f);
                if(kind==0)data[i]=Mathf.Clamp((low*.8f+Mathf.Sin(t*420)*.12f)*Mathf.Exp(-t*30),-1,1);
                else if(kind==1){float pulse=Mathf.Exp(-t*95)+(t>.22f?Mathf.Exp(-(t-.22f)*110):0);data[i]=(n*.38f+Mathf.Sin(t*2400)*.1f)*pulse;}
                else data[i]=Mathf.Sin(t*2*Mathf.PI*850)*Mathf.Exp(-t*110)*.24f;
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}
