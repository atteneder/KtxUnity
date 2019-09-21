using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#if BASISU_VERBOSE
using System.Text;
using Enum = System.Enum;
#endif

namespace BasisUniversalUnity {
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
                opaqueFormatDict.Add(GraphicsFormat.RGBA_BC7_SRGB,TranscodeFormat.BC7_M6_OPAQUE_ONLY);
                opaqueFormatDict.Add(GraphicsFormat.RGB_PVRTC_4Bpp_SRGB,TranscodeFormat.PVRTC1_4_OPAQUE_ONLY);
                opaqueFormatDict.Add(GraphicsFormat.RGB_ETC_UNorm,TranscodeFormat.ETC1);
                opaqueFormatDict.Add(GraphicsFormat.RGB_ETC2_SRGB,TranscodeFormat.ETC2);
                opaqueFormatDict.Add(GraphicsFormat.RGBA_DXT1_SRGB,TranscodeFormat.BC1);
                opaqueFormatDict.Add(GraphicsFormat.R_BC4_UNorm,TranscodeFormat.BC4);
                opaqueFormatDict.Add(GraphicsFormat.RG_BC5_UNorm,TranscodeFormat.BC5);
            }

            if(alphaFormatDict==null) {
                alphaFormatDict = new Dictionary<GraphicsFormat,TranscodeFormat>();
#if PLATFORM_IOS
                alphaFormatDict.Add(GraphicsFormat.RGBA_ETC2_SRGB,TranscodeFormat.ETC2);
#endif
                alphaFormatDict.Add(GraphicsFormat.RGBA_DXT5_SRGB,TranscodeFormat.BC3);
            }

            if(opaqueFormatLegacyDict==null) {
                opaqueFormatLegacyDict = new Dictionary<TextureFormat,TranscodeFormat>();
                opaqueFormatLegacyDict.Add(TextureFormat.BC7,TranscodeFormat.BC7_M6_OPAQUE_ONLY);
                opaqueFormatLegacyDict.Add(TextureFormat.PVRTC_RGB4,TranscodeFormat.PVRTC1_4_OPAQUE_ONLY);
                opaqueFormatLegacyDict.Add(TextureFormat.ETC_RGB4,TranscodeFormat.ETC1);
                opaqueFormatLegacyDict.Add(TextureFormat.ETC2_RGBA8,TranscodeFormat.ETC2);
                opaqueFormatLegacyDict.Add(TextureFormat.DXT1,TranscodeFormat.BC1);
                opaqueFormatLegacyDict.Add(TextureFormat.BC4,TranscodeFormat.BC4);
                opaqueFormatLegacyDict.Add(TextureFormat.BC5,TranscodeFormat.BC5);
            }

            if(alphaFormatLegacyDict==null) {
                alphaFormatLegacyDict = new Dictionary<TextureFormat,TranscodeFormat>();
                alphaFormatLegacyDict.Add(TextureFormat.DXT5,TranscodeFormat.BC3);
                alphaFormatLegacyDict.Add(TextureFormat.ETC2_RGBA8,TranscodeFormat.ETC2);            
                alphaFormatLegacyDict.Add(TextureFormat.ETC2_RGBA1,TranscodeFormat.ETC2); // Not sure if this works
            }
        }

        public static void Init() {
            if(!initialized) {
                InitInternal();
            }
        }

        public static bool GetPreferredFormat( bool isPowerOfTwo, bool isSquare, out GraphicsFormat unityFormat, out TranscodeFormat transcodeFormat ) {
            unityFormat = GraphicsFormat.RGBA_DXT1_SRGB;
            transcodeFormat = TranscodeFormat.BC1;

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
            transcodeFormat = TranscodeFormat.BC1;

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
            transcodeFormat = TranscodeFormat.BC1;

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
            transcodeFormat = TranscodeFormat.BC1;

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

#if BASISU_VERBOSE
        public static void CheckTextureSupport() {
            var sb = new StringBuilder();
            foreach(var format in opaqueFormatDict) {
                var supported = SystemInfo.IsFormatSupported(format.Key,FormatUsage.Sample);
                sb.AppendFormat("{0} support: {1}\n",format.Key,supported);
            }
            foreach(var format in alphaFormatDict) {
                var supported = SystemInfo.IsFormatSupported(format.Key,FormatUsage.Sample);
                sb.AppendFormat("(alpha) {0} support: {1}\n",format.Key,supported);
            }
            foreach(var format in opaqueFormatLegacyDict) {
                var supported = SystemInfo.SupportsTextureFormat(format.Key);
                sb.AppendFormat("legacy {0} support: {1}\n",format.Key,supported);
            }
            foreach(var format in alphaFormatLegacyDict) {
                var supported = SystemInfo.SupportsTextureFormat(format.Key);
                sb.AppendFormat("legacy (alpha) {0} support: {1}\n",format.Key,supported);
            }

            GraphicsFormat[] allGfxFormats = (GraphicsFormat[]) Enum.GetValues(typeof(GraphicsFormat));
            foreach(var format in allGfxFormats) {
                Log(
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

        [System.Diagnostics.Conditional("BASISU_VERBOSE")]
        static void Log(string format, params object[] args) {
            Debug.LogFormat(format,args);
        }
#endif
    }
}