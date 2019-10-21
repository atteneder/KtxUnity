// Copyright (c) 2019 Andreas Atteneder, All Rights Reserved.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#if KTX_VERBOSE
using System.Text;
using Enum = System.Enum;
#endif

namespace KtxUnity {
    public static class TranscodeFormatHelper
    {
        static bool initialized;
        static Dictionary<GraphicsFormat,TranscodeFormat> opaqueFormatDict;
        static Dictionary<GraphicsFormat,TranscodeFormat> alphaFormatDict;
        static Dictionary<TextureFormat,TranscodeFormat> opaqueFormatLegacyDict;
        static Dictionary<TextureFormat,TranscodeFormat> alphaFormatLegacyDict;

        static void InitInternal()
        {
            initialized=true;
            
            if(opaqueFormatDict==null) {
                opaqueFormatDict = new Dictionary<GraphicsFormat, TranscodeFormat>();
                
                // Compressed
                opaqueFormatDict.Add(GraphicsFormat.RGBA_BC7_SRGB,TranscodeFormat.BC7_M6_RGB);
                opaqueFormatDict.Add(GraphicsFormat.RGB_PVRTC_4Bpp_SRGB,TranscodeFormat.PVRTC1_4_RGB);
                opaqueFormatDict.Add(GraphicsFormat.RGB_ETC_UNorm,TranscodeFormat.ETC1_RGB);
                opaqueFormatDict.Add(GraphicsFormat.RGBA_DXT1_SRGB,TranscodeFormat.BC1_RGB);
                opaqueFormatDict.Add(GraphicsFormat.R_BC4_UNorm,TranscodeFormat.BC4_R);
                opaqueFormatDict.Add(GraphicsFormat.RG_BC5_UNorm,TranscodeFormat.BC5_RG);
                // opaqueFormatDict.Add(GraphicsFormat.?,TranscodeFormat.ATC_RGB);
                // opaqueFormatDict.Add(GraphicsFormat.?,TranscodeFormat.FXT1_RGB);
                // opaqueFormatDict.Add(GraphicsFormat.?,TranscodeFormat.PVRTC2_4_RGB);
                // opaqueFormatDict.Add(GraphicsFormat.?,TranscodeFormat.ETC2_EAC_R11);
                // opaqueFormatDict.Add(GraphicsFormat.?,TranscodeFormat.ETC2_EAC_RG11);
                
                // Uncompressed
                opaqueFormatDict.Add(GraphicsFormat.R5G6B5_UNormPack16,TranscodeFormat.RGB565);
                opaqueFormatDict.Add(GraphicsFormat.B5G6R5_UNormPack16,TranscodeFormat.BGR565);
            }

            if(alphaFormatDict==null) {
                alphaFormatDict = new Dictionary<GraphicsFormat,TranscodeFormat>();

                // Compressed
                alphaFormatDict.Add(GraphicsFormat.RGBA_BC7_SRGB,TranscodeFormat.BC7_M5_RGBA); // Did not work in editor
                alphaFormatDict.Add(GraphicsFormat.RGBA_PVRTC_4Bpp_SRGB,TranscodeFormat.PVRTC1_4_RGBA);
                alphaFormatDict.Add(GraphicsFormat.RGBA_ETC2_SRGB,TranscodeFormat.ETC2_RGBA);
                alphaFormatDict.Add(GraphicsFormat.RGBA_ASTC4X4_SRGB,TranscodeFormat.ASTC_4x4_RGBA);
                alphaFormatDict.Add(GraphicsFormat.RGBA_DXT5_SRGB,TranscodeFormat.BC3_RGBA);
                // alphaFormatDict.Add(GraphicsFormat.?,TranscodeFormat.ATC_RGBA);
                // alphaFormatDict.Add(GraphicsFormat.?,TranscodeFormat.PVRTC2_4_RGBA);

                // Uncompressed
                alphaFormatDict.Add(GraphicsFormat.R8G8B8A8_SRGB,TranscodeFormat.RGBA32);
                alphaFormatDict.Add(GraphicsFormat.R4G4B4A4_UNormPack16,TranscodeFormat.RGBA4444);
            }

            if(opaqueFormatLegacyDict==null) {
                opaqueFormatLegacyDict = new Dictionary<TextureFormat,TranscodeFormat>();
                
                // Compressed
                opaqueFormatLegacyDict.Add(TextureFormat.BC7,TranscodeFormat.BC7_M6_RGB);
                opaqueFormatLegacyDict.Add(TextureFormat.PVRTC_RGB4,TranscodeFormat.PVRTC1_4_RGB);
                opaqueFormatLegacyDict.Add(TextureFormat.ETC_RGB4,TranscodeFormat.ETC1_RGB);
                opaqueFormatLegacyDict.Add(TextureFormat.DXT1,TranscodeFormat.BC1_RGB);
                opaqueFormatLegacyDict.Add(TextureFormat.BC4,TranscodeFormat.BC4_R);
                opaqueFormatLegacyDict.Add(TextureFormat.BC5,TranscodeFormat.BC5_RG);
                // opaqueFormatLegacyDict.Add(TextureFormat.ETC_RGB4,TranscodeFormat.ATC_RGB);//Not sure if it works
                // opaqueFormatLegacyDict.Add(TextureFormat.?,TranscodeFormat.FXT1_RGB);
                // opaqueFormatLegacyDict.Add(TextureFormat.PVRTC_RGB4?,TranscodeFormat.PVRTC2_4_RGB);
                // opaqueFormatLegacyDict.Add(TextureFormat.?,TranscodeFormat.ETC2_EAC_R11);
                // opaqueFormatLegacyDict.Add(TextureFormat.?,TranscodeFormat.ETC2_EAC_RG11);

                // Uncompressed
                opaqueFormatLegacyDict.Add(TextureFormat.RGB565,TranscodeFormat.RGB565);
                // opaqueFormatLegacyDict.Add(TextureFormat.?,TranscodeFormat.BGR565);
            }

            if(alphaFormatLegacyDict==null) {
                alphaFormatLegacyDict = new Dictionary<TextureFormat,TranscodeFormat>();

                // Compressed
                alphaFormatLegacyDict.Add(TextureFormat.BC7,TranscodeFormat.BC7_M5_RGBA);
                alphaFormatLegacyDict.Add(TextureFormat.PVRTC_RGBA4,TranscodeFormat.PVRTC1_4_RGBA);
                alphaFormatLegacyDict.Add(TextureFormat.ETC2_RGBA8,TranscodeFormat.ETC2_RGBA);
                alphaFormatLegacyDict.Add(TextureFormat.ETC2_RGBA1,TranscodeFormat.ETC2_RGBA); // Not sure if this works
                alphaFormatLegacyDict.Add(TextureFormat.ASTC_4x4,TranscodeFormat.ASTC_4x4_RGBA);
                alphaFormatLegacyDict.Add(TextureFormat.DXT5,TranscodeFormat.BC3_RGBA);
                // alphaFormatLegacyDict.Add(TextureFormat.ETC2_RGBA8,TranscodeFormat.ATC_RGBA); // Not sure if this works
                // alphaFormatLegacyDict.Add(TextureFormat.?,TranscodeFormat.PVRTC2_4_RGBA);

                // Uncompressed
                alphaFormatLegacyDict.Add(TextureFormat.RGBA4444,TranscodeFormat.RGBA4444);
                alphaFormatLegacyDict.Add(TextureFormat.RGBA32,TranscodeFormat.RGBA32);
            }
        }

        public static void Init() {
            if(!initialized) {
                InitInternal();
#if KTX_VERBOSE
                CheckTextureSupport();
#endif
            }
        }

        public static bool GetFormatsForImage(
            IMetaData meta,
            ILevelInfo li,
            out GraphicsFormat graphicsFormat,
            out TextureFormat? textureFormat,
            out TranscodeFormat transF
            )
        {
            TextureFormat tf;
            bool match = false;
            graphicsFormat = GraphicsFormat.None;
            textureFormat = null;
            transF = meta.hasAlpha ? TranscodeFormat.RGBA32 : TranscodeFormat.RGB565;

            if(meta.hasAlpha) {
                if(TranscodeFormatHelper.GetPreferredFormatAlpha(li.isPowerOfTwo,li.isSquare,out graphicsFormat,out transF)) {
                    match = true;
                } else
                if(TranscodeFormatHelper.GetPreferredFormatLegacyAlpha(li.isPowerOfTwo,li.isSquare,out tf,out transF)) {
                    match = true;
                    textureFormat = tf;
                }
            }
            
            if( !meta.hasAlpha || !match ) {
                if(TranscodeFormatHelper.GetPreferredFormat(li.isPowerOfTwo,li.isSquare,out graphicsFormat,out transF)) {
                    match = true;
                } else
                if(TranscodeFormatHelper.GetPreferredFormatLegacy(li.isPowerOfTwo,li.isSquare,out tf,out transF)) {
                    match = true;
                    textureFormat = tf;
                }
            }
            return match;
        }
        
        public static bool GetPreferredFormat( bool isPowerOfTwo, bool isSquare, out GraphicsFormat unityFormat, out TranscodeFormat transcodeFormat ) {
            unityFormat = GraphicsFormat.RGBA_DXT1_SRGB;
            transcodeFormat = TranscodeFormat.BC1_RGB;

            foreach(var format in opaqueFormatDict) {
                if(
                    format.Key==GraphicsFormat.RGB_PVRTC_4Bpp_SRGB
                    && ( !isPowerOfTwo || !isSquare )
                ) {
                    continue;
                }
                var supported = SystemInfo.IsFormatSupported(format.Key,FormatUsage.Sample);
                if (supported) {
                    unityFormat = format.Key;
                    transcodeFormat = format.Value;
                    return true;
                }
            }
            return false;
        }

        public static bool GetPreferredFormatAlpha( bool isPowerOfTwo, bool isSquare, out GraphicsFormat unityFormat, out TranscodeFormat transcodeFormat ) {
            unityFormat = GraphicsFormat.RGBA_DXT1_SRGB;
            transcodeFormat = TranscodeFormat.BC1_RGB;

            foreach(var format in alphaFormatDict) {
                var supported = SystemInfo.IsFormatSupported(format.Key,FormatUsage.Sample);
                if (supported) {
                    unityFormat = format.Key;
                    transcodeFormat = format.Value;
                    return true;
                }
            }
            return false;
        }

        public static bool GetPreferredFormatLegacy( bool isPowerOfTwo, bool isSquare, out TextureFormat unityFormat, out TranscodeFormat transcodeFormat ) {
            unityFormat = TextureFormat.DXT1;
            transcodeFormat = TranscodeFormat.BC1_RGB;

            foreach(var format in opaqueFormatLegacyDict) {
                var supported = SystemInfo.SupportsTextureFormat(format.Key);
                if (supported) {
                    unityFormat = format.Key;
                    transcodeFormat = format.Value;
                    return true;
                }
            }
            return false;
        }

        public static bool GetPreferredFormatLegacyAlpha( bool isPowerOfTwo, bool isSquare, out TextureFormat unityFormat, out TranscodeFormat transcodeFormat ) {
            unityFormat = TextureFormat.DXT1;
            transcodeFormat = TranscodeFormat.BC1_RGB;

            foreach(var format in alphaFormatLegacyDict) {
                var supported = SystemInfo.SupportsTextureFormat(format.Key);
                if (supported) {
                    unityFormat = format.Key;
                    transcodeFormat = format.Value;
                    return true;
                }
            }
            return false;
        }

#if KTX_VERBOSE
        public static void CheckTextureSupport (
            
        ) {
            List<KeyValuePair<GraphicsFormat,TranscodeFormat>> graphicsFormats;
            List<KeyValuePair<TextureFormat,TranscodeFormat>> textureFormats;
            GetSupportedTextureFormats(out graphicsFormats,out textureFormats);
        }

        public static void GetSupportedTextureFormats (
            out List<KeyValuePair<GraphicsFormat,TranscodeFormat>> graphicsFormats,
            out List<KeyValuePair<TextureFormat,TranscodeFormat>> textureFormats
        )
        {
            graphicsFormats = new List<KeyValuePair<GraphicsFormat,TranscodeFormat>>();
            textureFormats = new List<KeyValuePair<TextureFormat,TranscodeFormat>>();

            var sb = new StringBuilder();
            foreach(var format in opaqueFormatDict) {
                var supported = SystemInfo.IsFormatSupported(format.Key,FormatUsage.Sample);
                if(supported) {
                    graphicsFormats.Add(format);
                }
                sb.AppendFormat("{0} support: {1}\n",format.Key,supported);
            }
            foreach(var format in alphaFormatDict) {
                var supported = SystemInfo.IsFormatSupported(format.Key,FormatUsage.Sample);
                if(supported) {
                    graphicsFormats.Add(format);
                }
                sb.AppendFormat("(alpha) {0} support: {1}\n",format.Key,supported);
            }
            foreach(var format in opaqueFormatLegacyDict) {
                var supported = SystemInfo.SupportsTextureFormat(format.Key);
                if(supported) {
                    textureFormats.Add(format);
                }
                sb.AppendFormat("legacy {0} support: {1}\n",format.Key,supported);
            }
            foreach(var format in alphaFormatLegacyDict) {
                var supported = SystemInfo.SupportsTextureFormat(format.Key);
                if(supported) {
                    textureFormats.Add(format);
                }
                sb.AppendFormat("legacy (alpha) {0} support: {1}\n",format.Key,supported);
            }

            Debug.Log(sb.ToString());

            sb.Clear();

            GraphicsFormat[] allGfxFormats = (GraphicsFormat[]) Enum.GetValues(typeof(GraphicsFormat));
            foreach(var format in allGfxFormats) {
                sb.AppendFormat(
                    "{0} sample:{1} blend:{2} getpixels:{3} linear:{4} loadstore:{5} aa2:{6} aa4:{7} aa8:{8} readpixels:{9} render:{10} setpixels:{11} sparse:{12}\n"
                    ,format
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.Sample)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.Blend)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.GetPixels)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.Linear)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.LoadStore)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.MSAA2x)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.MSAA4x)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.MSAA8x)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.ReadPixels)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.Render)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.SetPixels)?"1":"0"
                    ,SystemInfo.IsFormatSupported(format,FormatUsage.Sparse)?"1":"0"
                );
            }

            Debug.Log(sb.ToString());
        }

        [System.Diagnostics.Conditional("KTX_VERBOSE")]
        static void Log(string format, params object[] args) {
            Debug.LogFormat(format,args);
        }
#endif
    }
}