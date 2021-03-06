﻿#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using Celeste.Mod;
using Celeste.Mod.Helpers;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Monocle {
    class patch_VirtualTexture : patch_VirtualAsset {

        // We're effectively in VirtualAsset, but still need to "expose" private fields to our mod.
        public string Path { get; private set; }
        // This is public, but we don't build upon the original type. MonoMod knows what to do, though.
        public Texture2D Texture;

        public ModAsset Metadata { get; private set; }

        public VirtualTexture Fallback;

        // This _should_ work, but hell, this MonoModConstructor usage syntax went untested for ages. -ade
        [MonoModConstructor]
        internal patch_VirtualTexture(ModAsset metadata) {
            Metadata = metadata;
            Name = metadata.PathMapped;
            Reload();
        }

        internal extern void orig_Reload();
        internal override void Reload() {
            if (Metadata == null) {
                orig_Reload();
                return;
            }

            Unload();
            Texture = null;

            Stream stream = Metadata.Stream;
            if (stream != null) {
                using (stream)
                    Texture = Texture2D.FromStream(Celeste.Celeste.Instance.GraphicsDevice, stream);

                TextureMeta meta;
                if (Metadata.TryGetMeta(out meta)) {

                    if (!meta.Premultiplied)
                        Texture.Premultiply();

                } else {
                    // Assume unpremultiplied by default.
                    Texture.Premultiply();
                }

            } else if (Fallback != null) {
                ((patch_VirtualTexture) (object) Fallback).Reload();
                Texture = Fallback.Texture;
            }

            if (Texture != null) {
                Width = Texture.Width;
                Height = Texture.Height;
            }
        }

    }
    public static class VirtualTextureExt {

        // Mods can't access patch_ classes directly.
        // We thus expose any new members through extensions.

        /// <summary>
        /// If the VirtualTexture originates from a mod, get the mod asset metadata.
        /// </summary>
        public static ModAsset GetMetadata(this VirtualTexture self)
            => ((patch_VirtualTexture) (object) self).Metadata;

        /// <summary>
        /// Set a fallback texture in case the texture becomes unavailable on reload.
        /// </summary>
        public static void SetFallback(this VirtualTexture self, VirtualTexture fallback)
            => ((patch_VirtualTexture) (object) self).Fallback = fallback;

    }
}
