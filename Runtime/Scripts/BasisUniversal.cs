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

/// TODO: Re-using transcoders does not work consistently. Fix and enable!
// #define POOL_TRANSCODERS

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Experimental.Rendering;
using Unity.Jobs;
using Unity.Collections;
#if BASISU_VERBOSE
using System.Text;
using Enum = System.Enum;
#endif


namespace BasisUniversalUnity {

    public static class BasisUniversal
    {
    #if UNITY_EDITOR_OSX || UNITY_WEBGL || UNITY_IOS
        public const string INTERFACE_DLL = "__Internal";
    #elif UNITY_ANDROID || UNITY_STANDALONE
        public const string INTERFACE_DLL = "basisu";
    #endif

        /// <summary>
        /// Benchmarks have shown that the 4 frame limit until disposal that
        /// Allocator.TempJob grants is sometimes not enough, so I chose Persistent.
        /// </summary>
        public const Allocator defaultAllocator = Allocator.Persistent;

        static bool initialized;
        static int transcoderCountAvailable = 8;
        static Dictionary<GraphicsFormat,TranscodeFormat> opaqueFormatDict;
        static Dictionary<GraphicsFormat,TranscodeFormat> alphaFormatDict;
        static Dictionary<TextureFormat,TranscodeFormat> opaqueFormatLegacyDict;
        static Dictionary<TextureFormat,TranscodeFormat> alphaFormatLegacyDict;

#if POOL_TRANSCODERS
        static Stack<TranscoderInstance> transcoderPool;
#endif

        static void InitInternal()
        {
            initialized=true;
            aa_basis_init();

            transcoderCountAvailable = UnityEngine.SystemInfo.processorCount;

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

        public static TranscoderInstance GetTranscoderInstance() {
            if(!initialized) {
                InitInternal();
            }
#if POOL_TRANSCODERS
            if(transcoderPool!=null) {
                return transcoderPool.Pop();
            }
#endif
            if(transcoderCountAvailable>0) {
                transcoderCountAvailable--;
                return new TranscoderInstance(aa_create_basis());
            } else {
                return null;
            }
        }

        public static void ReturnTranscoderInstance( TranscoderInstance transcoder ) {
#if POOL_TRANSCODERS
            if(transcoderPool==null) {
                transcoderPool = new Stack<TranscoderInstance>();
            }
            transcoderPool.Push(transcoder);
#endif
            transcoderCountAvailable++;
        }

        public static bool GetFormats(
            MetaData meta,
            uint imageIndex,
            out GraphicsFormat graphicsFormat,
            out TextureFormat? textureFormat,
            out TranscodeFormat transF
        ) {
            graphicsFormat = GraphicsFormat.None;
            textureFormat = null;
            transF = TranscodeFormat.ETC1;

            ImageInfo ii = meta.images[imageIndex];
            LevelInfo li = ii.levels[0];

            TextureFormat tf;
            bool match = false;

            if(meta.hasAlpha) {
                if(GetPreferredFormatAlpha(li.isPowerOfTwo,li.isSquare,out graphicsFormat,out transF)) {
                    match = true;
                } else
                if(GetPreferredFormatLegacyAlpha(li.isPowerOfTwo,li.isSquare,out tf,out transF)) {
                    match = true;
                    textureFormat = tf;
                }
            }
            
            if( !meta.hasAlpha || match ) {
                if(GetPreferredFormat(li.isPowerOfTwo,li.isSquare,out graphicsFormat,out transF)) {
                    match = true;
                } else
                if(GetPreferredFormatLegacy(li.isPowerOfTwo,li.isSquare,out tf,out transF)) {
                    match = true;
                    textureFormat = tf;
                }
            }
            
            if(!match) {
                Debug.LogError("No supported format found!\nRebuild with BASISU_VERBOSE scripting define to debug.");
                #if BASISU_VERBOSE
                BasisUniversal.CheckTextureSupport();
                #endif
            }

            return match;
        }

        public unsafe static JobHandle LoadBytesJob(
            ref BasisUniversalJob job,
            TranscoderInstance basis,
            NativeArray<byte> basisuData,
            TranscodeFormat transF
        ) {
            
            Profiler.BeginSample("BasisU.LoadBytesJob");
            
            var size = basis.GetImageTranscodedSize(0,0,transF);

            job.format = transF;
            job.size = size;
            job.nativeReference = basis.nativeReference;
            
            job.textureData = new NativeArray<byte>((int)size,defaultAllocator);

            var jobHandle = job.Schedule();

            Profiler.EndSample();
            return jobHandle;
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
#endif

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

        [System.Diagnostics.Conditional("BASISU_VERBOSE")]
        static void Log(string format, params object[] args) {
            Debug.LogFormat(format,args);
        }

        [DllImport(INTERFACE_DLL)]
        private static extern void aa_basis_init();

        [DllImport(INTERFACE_DLL)]
        private static unsafe extern System.IntPtr aa_create_basis();
    }
}