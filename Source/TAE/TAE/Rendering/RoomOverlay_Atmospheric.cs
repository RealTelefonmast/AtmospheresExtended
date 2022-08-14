using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE.Rendering
{
    [StaticConstructorOnStartup]
    public class RoomOverlay_Atmospheric : RoomOverlayRenderer
    {
        public static readonly Texture2D Nebula1 = ContentFinder<Texture2D>.Get("RoomOverlay/Liquid/Water1", true);
        public static readonly Texture2D Nebula2 = ContentFinder<Texture2D>.Get("RoomOverlay/Liquid/Water2", true);

        [TweakValue("Atmospheric_BlendSpeed", 0f, 2f)]
        public static float BlendSpeed = 0.4f;
        [TweakValue("Atmospheric_BlendValue", 0f, 1f)]
        public static float BlendValue = 0.48f;
        [TweakValue("Atmospheric_Alpha", 0f, 1f)]
        public static float Alpha = 1f;

        public override Shader Shader => AtmosContent.TextureBlend;

        protected override void InitShaderProps(Material material)
        {
            base.InitShaderProps(material);
            material.SetTexture("_MainTex1", Nebula1);
            material.SetTexture("_MainTex2", Nebula2);
            material.SetColor("_Color", new ColorInt(0, 255, 255, 100).ToColor);
        }

        public void UpdateTick()
        {
            UpdateShaderProps(Material);
        }

        protected override void UpdateShaderProps(Material material)
        {
            base.UpdateShaderProps(material);
            material.SetFloat("_BlendValue", BlendValue);
            material.SetFloat("_BlendSpeed", BlendSpeed);
            material.SetFloat("_Opacity", MainAlpha * Alpha);
        }
        
    }
}
