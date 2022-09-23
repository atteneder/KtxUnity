// Copyright (c) 2019-2022 Andreas Atteneder, All Rights Reserved.

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
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare,
                    GraphicsFormat.RGBA_BC7_SRGB,
                    TranscodeFormat.BC7_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare | TextureFeatures.Linear,
                    GraphicsFormat.RGBA_BC7_UNorm,
                    TranscodeFormat.BC7_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare,
                    GraphicsFormat.RGBA_DXT5_SRGB,
                    TranscodeFormat.BC3_RGBA));

                allFormats.Add( new FormatInfo(
                    TextureFeatures.AlphaChannel | TextureFeatures.NonPowerOfTwo | TextureFeatures.NonSquare | TextureFeatures.Linear,
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

        /// <summary>
        /// Returns a fitting <see cref="TextureFormat"/> for a given
        /// <see cref="GraphicsFormat"/>
        /// </summary>
        /// <param name="graphicsFormat">Input format</param>
        /// <returns>Fitting <see cref="TextureFormat"/></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static TextureFormat GetTextureFormat(GraphicsFormat graphicsFormat) {
            switch (graphicsFormat) {
                case GraphicsFormat.None:
                case GraphicsFormat.R8_SRGB:
                case GraphicsFormat.R8_UNorm:
                case GraphicsFormat.R8_SNorm:
                case GraphicsFormat.R8_UInt:
                case GraphicsFormat.R8_SInt:
                    return TextureFormat.R8;
                case GraphicsFormat.R8G8_SRGB:
                case GraphicsFormat.R8G8_UNorm:
                case GraphicsFormat.R8G8_SNorm:
                case GraphicsFormat.R8G8_UInt:
                case GraphicsFormat.R8G8_SInt:
                    return TextureFormat.RG16;
                case GraphicsFormat.R8G8B8_SRGB:
                case GraphicsFormat.R8G8B8_UNorm:
                case GraphicsFormat.R8G8B8_SNorm:
                case GraphicsFormat.R8G8B8_UInt:
                case GraphicsFormat.R8G8B8_SInt:
                    return TextureFormat.RGB24;
                case GraphicsFormat.R8G8B8A8_SRGB:
                case GraphicsFormat.R8G8B8A8_UNorm:
                case GraphicsFormat.R8G8B8A8_SNorm:
                case GraphicsFormat.R8G8B8A8_UInt:
                case GraphicsFormat.R8G8B8A8_SInt:
                    return TextureFormat.RGBA32;
                case GraphicsFormat.R16_UNorm:
                case GraphicsFormat.R16_SNorm:
                case GraphicsFormat.R16_UInt:
                case GraphicsFormat.R16_SInt:
                    return TextureFormat.R16;
                case GraphicsFormat.R16G16_UNorm:
                case GraphicsFormat.R16G16_SNorm:
                case GraphicsFormat.R16G16_UInt:
                case GraphicsFormat.R16G16_SInt:
                    return TextureFormat.RG16;
                case GraphicsFormat.R16G16B16_UNorm:
                case GraphicsFormat.R16G16B16_SNorm:
                case GraphicsFormat.R16G16B16_UInt:
                case GraphicsFormat.R16G16B16_SInt:
                    return TextureFormat.RGB48;
                case GraphicsFormat.R16G16B16A16_UNorm:
                case GraphicsFormat.R16G16B16A16_SNorm:
                case GraphicsFormat.R16G16B16A16_UInt:
                case GraphicsFormat.R16G16B16A16_SInt:
                    return TextureFormat.RGBA64;
                case GraphicsFormat.R32_SFloat:
                    return TextureFormat.RFloat;
                case GraphicsFormat.R32G32_SFloat:
                    return TextureFormat.RGFloat;
                case GraphicsFormat.R16_SFloat:
                    return TextureFormat.RHalf;
                case GraphicsFormat.R16G16_SFloat:
                    return TextureFormat.RG32;
                case GraphicsFormat.R16G16B16_SFloat: // TODO: wrong?
                case GraphicsFormat.R16G16B16A16_SFloat:
                    return TextureFormat.RGBAHalf;
                case GraphicsFormat.R32G32B32_SFloat: // TODO: wrong?
                case GraphicsFormat.R32G32B32A32_SFloat:
                    return TextureFormat.RGBAFloat;
                case GraphicsFormat.B8G8R8_SRGB: // TODO: wrong channels
                case GraphicsFormat.B8G8R8_UNorm: // TODO: wrong channels
                case GraphicsFormat.B8G8R8_SNorm: // TODO: wrong channels
                case GraphicsFormat.B8G8R8_UInt: // TODO: wrong channels
                case GraphicsFormat.B8G8R8_SInt: // TODO: wrong channels
                    return TextureFormat.RGB24;
                case GraphicsFormat.B8G8R8A8_SRGB:
                case GraphicsFormat.B8G8R8A8_UNorm:
                case GraphicsFormat.B8G8R8A8_SNorm:
                case GraphicsFormat.B8G8R8A8_UInt:
                case GraphicsFormat.B8G8R8A8_SInt:
                    return TextureFormat.BGRA32;
                case GraphicsFormat.R4G4B4A4_UNormPack16:
                case GraphicsFormat.B4G4R4A4_UNormPack16:
                    return TextureFormat.RGBA4444;
                case GraphicsFormat.R5G6B5_UNormPack16:
                case GraphicsFormat.B5G6R5_UNormPack16:// TODO: Wrong channel order!
                    return TextureFormat.RGB565;
                case GraphicsFormat.RGBA_DXT1_SRGB:
                case GraphicsFormat.RGBA_DXT1_UNorm:
                    return TextureFormat.DXT1;
                case GraphicsFormat.RGBA_DXT5_SRGB:
                case GraphicsFormat.RGBA_DXT5_UNorm:
                    return TextureFormat.DXT5;
                case GraphicsFormat.R_BC4_UNorm:
                case GraphicsFormat.R_BC4_SNorm:
                    return TextureFormat.BC4;
                case GraphicsFormat.RG_BC5_UNorm:
                case GraphicsFormat.RG_BC5_SNorm:
                    return TextureFormat.BC5;
                case GraphicsFormat.RGB_BC6H_UFloat:
                case GraphicsFormat.RGB_BC6H_SFloat:
                    return TextureFormat.BC6H;
                case GraphicsFormat.RGBA_BC7_SRGB:
                case GraphicsFormat.RGBA_BC7_UNorm:
                    return TextureFormat.BC7;
                case GraphicsFormat.RGB_PVRTC_2Bpp_SRGB:
                case GraphicsFormat.RGB_PVRTC_2Bpp_UNorm:
                    return TextureFormat.PVRTC_RGB2;
                case GraphicsFormat.RGB_PVRTC_4Bpp_SRGB:
                case GraphicsFormat.RGB_PVRTC_4Bpp_UNorm:
                    return TextureFormat.PVRTC_RGB4;
                case GraphicsFormat.RGBA_PVRTC_2Bpp_SRGB:
                case GraphicsFormat.RGBA_PVRTC_2Bpp_UNorm:
                    return TextureFormat.PVRTC_RGBA2;
                case GraphicsFormat.RGBA_PVRTC_4Bpp_SRGB:
                case GraphicsFormat.RGBA_PVRTC_4Bpp_UNorm:
                    return TextureFormat.PVRTC_RGBA4;
                case GraphicsFormat.RGB_ETC_UNorm:
                    return TextureFormat.ETC_RGB4;
                case GraphicsFormat.RGB_ETC2_SRGB:
                case GraphicsFormat.RGB_ETC2_UNorm:
                    return TextureFormat.ETC2_RGB;
                case GraphicsFormat.RGB_A1_ETC2_SRGB:
                case GraphicsFormat.RGB_A1_ETC2_UNorm:
                    return TextureFormat.ETC2_RGBA1;
                case GraphicsFormat.RGBA_ETC2_SRGB:
                case GraphicsFormat.RGBA_ETC2_UNorm:
                    return TextureFormat.ETC2_RGBA8;
                case GraphicsFormat.R_EAC_UNorm:
                case GraphicsFormat.R_EAC_SNorm:
                    return TextureFormat.EAC_R;
                case GraphicsFormat.RG_EAC_UNorm:
                    return TextureFormat.EAC_RG;
                case GraphicsFormat.RG_EAC_SNorm:
                    return TextureFormat.EAC_RG_SIGNED;
                case GraphicsFormat.RGBA_ASTC4X4_SRGB:
                case GraphicsFormat.RGBA_ASTC4X4_UNorm:
                    return TextureFormat.ASTC_RGBA_4x4;
                case GraphicsFormat.RGBA_ASTC5X5_SRGB:
                case GraphicsFormat.RGBA_ASTC5X5_UNorm:
                    return TextureFormat.ASTC_RGBA_5x5;
                case GraphicsFormat.RGBA_ASTC6X6_SRGB:
                case GraphicsFormat.RGBA_ASTC6X6_UNorm:
                    return TextureFormat.ASTC_RGBA_6x6;
                case GraphicsFormat.RGBA_ASTC8X8_SRGB:
                case GraphicsFormat.RGBA_ASTC8X8_UNorm:
                    return TextureFormat.ASTC_RGBA_8x8;
                case GraphicsFormat.RGBA_ASTC10X10_SRGB:
                case GraphicsFormat.RGBA_ASTC10X10_UNorm:
                    return TextureFormat.ASTC_RGBA_10x10;
                case GraphicsFormat.RGBA_ASTC12X12_SRGB:
                case GraphicsFormat.RGBA_ASTC12X12_UNorm:
                    return TextureFormat.ASTC_RGBA_12x12;
                case GraphicsFormat.E5B9G9R9_UFloatPack32:
                    return TextureFormat.RGB9e5Float;

                // Unknown/Unsupported
                case GraphicsFormat.A10R10G10B10_XRSRGBPack32:
                case GraphicsFormat.A10R10G10B10_XRUNormPack32:
                case GraphicsFormat.A1R5G5B5_UNormPack16:
                case GraphicsFormat.A2B10G10R10_SIntPack32:
                case GraphicsFormat.A2B10G10R10_UIntPack32:
                case GraphicsFormat.A2B10G10R10_UNormPack32:
                case GraphicsFormat.A2R10G10B10_SIntPack32:
                case GraphicsFormat.A2R10G10B10_UIntPack32:
                case GraphicsFormat.A2R10G10B10_UNormPack32:
                case GraphicsFormat.A2R10G10B10_XRSRGBPack32:
                case GraphicsFormat.A2R10G10B10_XRUNormPack32:
                case GraphicsFormat.B10G11R11_UFloatPack32:
                case GraphicsFormat.B5G5R5A1_UNormPack16:
                case GraphicsFormat.R10G10B10_XRSRGBPack32:
                case GraphicsFormat.R10G10B10_XRUNormPack32:
                case GraphicsFormat.R32G32B32A32_SInt:
                case GraphicsFormat.R32G32B32A32_UInt:
                case GraphicsFormat.R32G32B32_SInt:
                case GraphicsFormat.R32G32B32_UInt:
                case GraphicsFormat.R32G32_SInt:
                case GraphicsFormat.R32G32_UInt:
                case GraphicsFormat.R32_SInt:
                case GraphicsFormat.R32_UInt:
                case GraphicsFormat.R5G5B5A1_UNormPack16:
                case GraphicsFormat.RGBA_DXT3_SRGB: // BC2
                case GraphicsFormat.RGBA_DXT3_UNorm: // BC2
                default:
                    throw new ArgumentOutOfRangeException(nameof(graphicsFormat), graphicsFormat, null);
            }
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