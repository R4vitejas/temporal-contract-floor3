using System;
using UnityEditor;
using UnityEngine;

namespace TemporalContract.Environment.EditorTools
{
    /// <summary>Shared retro palette for the existing source geometry.</summary>
    public static class RetroFloorStyle
    {
        const string Root = "Assets/Project/Environment/Materials/Retro";
        public static string Key(FullFloorBuilder.SourceMaterial source)
        {
            string n = source.sourceName.ToLowerInvariant();
            if (n.Contains("chalk") || n.Contains("board")) return "Board";
            if (n.Contains("screen") || n.Contains("black-plastic") || n.Contains("hitam")) return "Ink";
            string texture=(source.baseTextureSource??"").ToLowerInvariant();
            if(texture.Contains("wood"))return "Wood";
            if(texture.Contains("granite")||texture.Contains("marble"))return "Floor";
            if (n.Contains("tile") || n.Contains("granite") || n.Contains("marble")) return "Floor";
            if (n.Contains("roof")) return "Ceiling";
            if (n.Contains("light")) return "Light";
            if (n.Contains("wood") || n.Contains("door")) return "Wood";
            if (n.Contains("glass")) return "Glass";
            if (n.Contains("metal") || n.Contains("chrome") || n.Contains("cage") || n.Contains("frame") || n.Contains("blade")) return "Metal";
            if (n.Contains("red")) return "Brick";
            if (n.Contains("wall") || n.Contains("ceramic") || n.Contains("plastic") || n.Contains("putih")) return "Wall";
            float r=source.color[0],g=source.color[1],b=source.color[2];
            if (r>g*1.45f && r>b*1.45f) return "Brick";
            if (b>r*1.3f && b>g*1.2f) return "Blue";
            if (g>r*1.3f && g>b*1.1f) return "Board";
            if (r+g+b<.22f) return "Ink";
            if (r>g*1.1f && g>b*1.1f) return "Wood";
            return "Wall";
        }

        public static Material Material(string key)
        {
            System.IO.Directory.CreateDirectory(Root);
            string path=Root+"/Retro_"+key+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) {mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            Color color;
            switch(key)
            {
                case "Floor":color=new Color32(132,145,140,255);break;
                case "Ceiling":color=new Color32(190,184,163,255);break;
                case "Wood":color=new Color32(157,105,62,255);break;
                case "Board":color=new Color32(41,78,65,255);break;
                case "Ink":color=new Color32(36,44,48,255);break;
                case "Metal":color=new Color32(78,91,96,255);break;
                case "Brick":color=new Color32(164,67,53,255);break;
                case "Blue":color=new Color32(63,103,131,255);break;
                case "Glass":color=new Color32(131,174,177,255);break;
                case "Light":color=new Color32(246,225,170,255);break;
                default:color=new Color32(210,203,174,255);break;
            }
            string texturePath=Root+"/Retro_"+key+".asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture){texture=new Texture2D(16,16,TextureFormat.RGBA32,false);AssetDatabase.CreateAsset(texture,texturePath);}
            var pixels=new Color[256];
            for(int y=0;y<16;y++)for(int x=0;x<16;x++)
            {
                float shade=1f;
                if(key=="Floor")shade=(x==0 || y==0)?.82f:((x+y)%7==0?.97f:1f);
                else if(key=="Wood")shade=(y%4==0)?.88f:((x+3*y)%11==0?1.06f:1f);
                else shade=((x*7+y*11)%31==0)?.97f:1f;
                pixels[y*16+x]=new Color(color.r*shade,color.g*shade,color.b*shade,1f);
            }
            texture.SetPixels(pixels);texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Repeat;texture.Apply();EditorUtility.SetDirty(texture);
            mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",texture);mat.SetFloat("_Smoothness",.02f);mat.SetFloat("_Metallic",0f);mat.SetFloat("_Cull",0f);
            if(key=="Light"){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.35f);}
            else{mat.DisableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.black);}
            EditorUtility.SetDirty(mat);return mat;
        }
    }
}
