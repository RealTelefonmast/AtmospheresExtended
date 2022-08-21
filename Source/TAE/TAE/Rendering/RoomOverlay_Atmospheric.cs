using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    public class RoomOverlay_Atmospheric : RoomOverlayRenderer
    {
        private Dictionary<AtmosphericDef, Material> materialsByDef;

        [TweakValue("AE.RoomOverlay_BlendSpeed", 0f, 2f)]
        public static float BlendSpeed = 0.4f;
        [TweakValue("AE.RoomOverlay_BlendValue", 0f, 1f)]
        public static float BlendValue = 0.48f;
        [TweakValue("AE.RoomOverlay_Alpha", 0f, 1f)]
        public static float Alpha = 1f;

        [TweakValue("AE.RoomOverlay_SrcMode", 0f, 10f)]
        public static int SrcMode = 2;

        public override Shader Shader => AtmosContent.TextureBlend;

        public Dictionary<AtmosphericDef, Material>.KeyCollection Defs => materialsByDef.Keys;

        public RoomOverlay_Atmospheric()
        {
            materialsByDef = new();
        }

        public void TryRegisterNewOverlayPart(AtmosphericDef def)
        {
            if (def.roomOverlay == null) return;
            if (materialsByDef.ContainsKey(def)) return;
            TeleUpdateManager.Notify_EnqueueNewSingleAction(() => GetMaterial(def));
        }

        public Material GetMaterial(AtmosphericDef def)
        {
            if (!materialsByDef.TryGetValue(def, out var mat))
            {
                mat = new Material(AtmosContent.TextureBlend);
                InitShaderProps(mat, def);
                materialsByDef.Add(def, mat);
            }
            UpdateShaderProps(mat, def);
            return mat;
        }

        protected void InitShaderProps(Material material, AtmosphericDef def)
        {
            TeleUpdateManager.Notify_EnqueueNewSingleAction(() =>
            {
                var overlayPart1 = ContentFinder<Texture2D>.Get(def.roomOverlay.overlayTex1);
                var overlayPart2 = ContentFinder<Texture2D>.Get(def.roomOverlay.overlayTex2);
                material.SetTexture("_MainTex1", overlayPart1);
                material.SetTexture("_MainTex2", overlayPart2);
            });
            var color = def.roomOverlay.color;
            color.a = 100f / 255f;
            material.SetFloat("_SrcMode", 2);
            material.SetFloat("_DstMode", 1);
            material.SetColor("_Color", color);
        }

        public void UpdateTick()
        {
            if (!materialsByDef.Any()) return;
            foreach (var matDef in materialsByDef)
            {
                UpdateShaderProps(matDef.Value, matDef.Key);
            }
        }

        protected void UpdateShaderProps(Material material, AtmosphericDef def)
        {
            base.UpdateShaderProps(material);
            material.SetFloat("_SrcMode", SrcMode);
            material.SetFloat("_BlendValue", BlendValue);
            material.SetFloat("_BlendSpeed", BlendSpeed);
            material.SetFloat("_Opacity", MainAlpha * Alpha);
        }

        public void DrawFor(AtmosphericDef def, Vector3 drawPos, float saturation)
        {
            if (cachedMesh == null) return;
            MainAlpha = saturation;

            Matrix4x4 matrix = default;
            matrix.SetTRS(drawPos, Quaternion.identity, Vector3.one);
            Graphics.DrawMesh(cachedMesh, matrix, GetMaterial(def), 0);
        }
        
    }
}
