// Copyright (c) 2019-2021 Andreas Atteneder, All Rights Reserved.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#if KTX_VERBOSE
using System.Text;
using Enum = System.Enum;
#endif

namespace KtxUnity {

    /// <summary>
    /// Mask of texture features
    /// </summary>
    [Flags]
    enum TextureFeatures {
        None = 0x0,

        /// <summary>
        /// Format with 4 channels (RGB+Alpha)
        /// </summary>
        AlphaChannel = 0x1,

        /// <summary>
        /// Format supports arbitrary resolutions
        /// </summary>
        NonPowerOfTwo = 0x2,

        /// <summary>
        /// Format supports arbitrary resolutions
        /// </summary>
        NonMultipleOfFour = 0x4,

        /// <summary>
        /// Non square resolution
        /// </summary>
        NonSquare = 0x8,

        /// <summary>
        /// Linear value encoding (not sRGB)
        /// </summary>
        Linear = 0x10
    }

    public struct TranscodeFormatTuple {
        public GraphicsFormat format;
        public TranscodeFormat transcodeFormat;

        public TranscodeFormatTuple(GraphicsFormat format, TranscodeFormat transcodeFormat) {
            this.format = format;
            this.transcodeFormat = transcodeFormat;
        }
    }

    struct FormatInfo {
        public TextureFeatures features;
        public TranscodeFormatTuple formats; 

        public FormatInfo(TextureFeatures features, GraphicsFormat format, TranscodeFormat transcodeFormat ) {
            this.features = features;
            this.formats = new TranscodeFormatTuple(format,transcodeFormat);
        }
    }

    public static class TranscodeFormatHelper
    {
        static bool initialized;
        static Dictionary<TextureFeatures,TranscodeFormatTuple> formatCache;
        static List<FormatInfo> allFormats;

        static void InitInternal()
        {
            initialized=true;
            formatCache = new Dictionary<TextureFeatures, TranscodeFormatTuple>();
            
            if(allFormats==null) {

                allFormats = new List<FormatInfo>();
                
                // Add all formats into the List ordered. First supported match will be used.
                // This particular order is based on memory size (1st degree)
                // and a combination of quality and transcode speed (2nd degree)
                // source <http://richg42.blogspot.com/2018/05/basis-universal-gpu-texture-format.html>

                // Compressed
                allFormats.Add( new FormatInfo(
                    TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare,
                    GraphicsFormat.RGB_ETC2_SRGB,
                    TranscodeFormat.ETC1_RGB));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.RGB_ETC_UNorm,
                    TranscodeFormat.ETC1_RGB));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare,
#if UNITY_2018_3_OR_NEWER
                    GraphicsFormat.RGBA_DXT1_SRGB,
#else
                    GraphicsFormat.RGB_DXT1_SRGB,
#endif
                    TranscodeFormat.BC1_RGB));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare | TextureFeatures.Linear,
#if UNITY_2018_3_OR_NEWER
                    GraphicsFormat.RGBA_DXT1_UNorm,
#else
                    GraphicsFormat.RGB_DXT1_UNorm,
#endif
                    TranscodeFormat.BC1_RGB));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.NonMultipleOfFour,
                    GraphicsFormat.RGB_PVRTC_4Bpp_SRGB,
                    TranscodeFormat.PVRTC1_4_RGB));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.NonMultipleOfFour,
                    GraphicsFormat.RGB_PVRTC_4Bpp_UNorm,
                    TranscodeFormat.PVRTC1_4_RGB));

                // Compressed with alpha channel
                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare,
                    GraphicsFormat.RGBA_ASTC4X4_SRGB,
                    TranscodeFormat.ASTC_4x4_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.RGBA_ASTC4X4_UNorm,
                    TranscodeFormat.ASTC_4x4_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare,
                    GraphicsFormat.RGBA_ETC2_SRGB,
                    TranscodeFormat.ETC2_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.RGBA_ETC2_UNorm,
                    TranscodeFormat.ETC2_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare,
                    GraphicsFormat.RGBA_BC7_SRGB,
                    TranscodeFormat.BC7_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.RGBA_BC7_UNorm,
                    TranscodeFormat.BC7_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare,
                    GraphicsFormat.RGBA_DXT5_SRGB,
                    TranscodeFormat.BC3_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.RGBA_DXT5_UNorm,
                    TranscodeFormat.BC3_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonMultipleOfFour,
                    GraphicsFormat.RGBA_PVRTC_4Bpp_SRGB,
                    TranscodeFormat.PVRTC1_4_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.Linear | TextureFeatures.NonMultipleOfFour,
                    GraphicsFormat.RGBA_PVRTC_4Bpp_UNorm,
                    TranscodeFormat.PVRTC1_4_RGBA));

                // Uncompressed
                allFormats.Add( new FormatInfo(
                    TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.R5G6B5_UNormPack16,
                    TranscodeFormat.RGB565));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.B5G6R5_UNormPack16,
                    TranscodeFormat.BGR565));

                // Uncompressed with alpha channel
                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.R4G4B4A4_UNormPack16,
                    TranscodeFormat.RGBA4444));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare,
                    GraphicsFormat.R8G8B8A8_SRGB, // Also supports SNorm, UInt, SInt
                    TranscodeFormat.RGBA32));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonMultipleOfFour | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.R8G8B8A8_UNorm, // Also supports SNorm, UInt, SInt
                    TranscodeFormat.RGBA32));

                // Need to extend TextureFeatures to request single/dual channel texture formats.
                // Until then, those formats are at the end of the list
                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.R_EAC_UNorm, // Also supports SNorm
                    TranscodeFormat.ETC2_EAC_R11));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.RG_EAC_UNorm, // Also supports SNorm
                    TranscodeFormat.ETC2_EAC_RG11));

                // GraphicsFormat.RGB_A1_ETC2_SRGB,TranscodeFormat.ETC2_RGBA // Does not work; always transcodes 8-bit alpha
                // GraphicsFormat.RGBA_ETC2_SRGB,TranscodeFormat.ATC_RGBA // Not sure if this works (maybe compatible) - untested
                // GraphicsFormat.RGB_ETC_UNorm,ATC_RGB // Not sure if this works (maybe compatible) - untested

                // Not supported via KTX atm
                // GraphicsFormat.R_BC4_UNorm,TranscodeFormat.BC4_R
                // GraphicsFormat.RG_BC5_UNorm,TranscodeFormat.BC5_RG

                // Supported by BasisU, but no matching Unity GraphicsFormat
                // GraphicsFormat.?,TranscodeFormat.ATC_RGB
                // GraphicsFormat.?,TranscodeFormat.ATC_RGBA
                // GraphicsFormat.?,TranscodeFormat.FXT1_RGB
                // GraphicsFormat.?,TranscodeFormat.PVRTC2_4_RGB
                // GraphicsFormat.?,TranscodeFormat.PVRTC2_4_RGBA
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

        public static TranscodeFormatTuple? GetFormatsForImage(
            IMetaData meta,
            ILevelInfo li,
            bool linear = false
            )
        {
            TranscodeFormatTuple? formats;

            formats = TranscodeFormatHelper.GetPreferredFormat(
                meta.hasAlpha,
                li.isPowerOfTwo,
                li.isMultipleOfFour,
                li.isSquare,
                linear
                );
            
            if( !formats.HasValue && meta.hasAlpha ) {
                // Fall back to transcode RGB-only to RGBA texture
                formats = TranscodeFormatHelper.GetPreferredFormat(
                    false,
                    li.isPowerOfTwo,
                    li.isMultipleOfFour,
                    li.isSquare,
                    linear
                    );
            }
            return formats;
        }
        
        public static TranscodeFormatTuple? GetPreferredFormat(
            bool hasAlpha,
            bool isPowerOfTwo,
            bool isMultipleOfFour,
            bool isSquare,
            bool isLinear = false
        ) {
            TextureFeatures features = TextureFeatures.None;
            if(hasAlpha) {
                features |= TextureFeatures.AlphaChannel;
            }
            if(!isPowerOfTwo) {
                features |= TextureFeatures.NonPowerOfTwo;
            }
            if(!isMultipleOfFour) {
                features |= TextureFeatures.NonMultipleOfFour;
            }
            if(!isSquare) {
                features |= TextureFeatures.NonSquare;
            }
            if(isLinear) {
                features |= TextureFeatures.Linear;
            }

            TranscodeFormatTuple formatTuple;
            if(formatCache.TryGetValue(features,out formatTuple)) {
                return formatTuple;
            } else {
                foreach(var formatInfo in allFormats) {
                    if (!FormatIsMatch(features,formatInfo.features)) {
                        continue;
                    }
                    var supported = SystemInfo.IsFormatSupported(formatInfo.formats.format ,FormatUsage.Sample);
                    if (supported) {
                        formatCache[features] = formatInfo.formats;
                        return formatInfo.formats;
                    }
                }
#if DEBUG
                Debug.LogErrorFormat("Could not find transcode texture format! (alpha:{0} Po2:{1} sqr:{2})",hasAlpha,isPowerOfTwo,isSquare);
#endif
                return null;
            }
        }

        static bool FormatIsMatch(TextureFeatures required, TextureFeatures provided ) {
            return (required & provided) == required;
        }

#if KTX_VERBOSE
        public static void CheckTextureSupport () {
            // Dummy call to force logging all supported formats to console
            List<TranscodeFormatTuple> graphicsFormats;
            List<KeyValuePair<TextureFormat,TranscodeFormat>> textureFormats;
            GetSupportedTextureFormats(out graphicsFormats,out textureFormats);
        }

        public static void GetSupportedTextureFormats (
            out List<TranscodeFormatTuple> graphicsFormats,
            out List<KeyValuePair<TextureFormat,TranscodeFormat>> textureFormats
        )
        {
            graphicsFormats = new List<TranscodeFormatTuple>();
            textureFormats = new List<KeyValuePair<TextureFormat,TranscodeFormat>>();

            var sb = new StringBuilder();
            foreach(var formatInfo in allFormats) {
                var supported = SystemInfo.IsFormatSupported(formatInfo.formats.format,FormatUsage.Sample);
                if(supported) {
                    graphicsFormats.Add(formatInfo.formats);
                }
                sb.AppendFormat("{0} support: {1}\n",formatInfo.formats.format,supported);
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