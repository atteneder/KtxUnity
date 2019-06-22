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
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#if BASISU_VERBOSE
using System.Text;
#endif


namespace BasisUniversalUnity {

    public static class BasisUniversal
    {
    #if UNITY_EDITOR || UNITY_WEBGL || UNITY_IOS
        const string INTERFACE_DLL = "__Internal";
    #elif UNITY_ANDROID || UNITY_STANDALONE
        public const string INTERFACE_DLL = "basisu";
    #endif

        // source: C++ enum transcoder_texture_format
        public enum TranscodeFormat : uint {
            ETC1,
            BC1,
            BC4,
            PVRTC1_4_OPAQUE_ONLY,
            BC7_M6_OPAQUE_ONLY,
            ETC2,
            BC3,
            BC5,
        }

        public class BasisTexture {
            IntPtr nativeReference;

            public BasisTexture( IntPtr nativeReference ) {
                this.nativeReference = nativeReference;
            }

            public bool GetHasAlpha() {
                return aa_getHasAlpha(nativeReference);
            }

            public uint GetImageCount() {
                return aa_getNumImages(nativeReference);
            }

            public uint GetLevelCount(uint imageIndex) {
                return aa_getNumLevels(nativeReference,imageIndex);
            }

            public void GetImageSize( out uint width, out uint height, System.UInt32 image_index = 0, System.UInt32 level_index = 0) {
                width = aa_getImageWidth(nativeReference,image_index,level_index);
                height = aa_getImageHeight(nativeReference,image_index,level_index);
            }

            uint GetImageTranscodedSize(uint imageIndex, uint levelIndex, TranscodeFormat format) {
                return aa_getImageTranscodedSizeInBytes(nativeReference,imageIndex,levelIndex,(uint)format);
            }

            public unsafe bool Transcode(uint imageIndex,uint levelIndex,TranscodeFormat format,out byte[] transcodedData ) {
                
                transcodedData = null;

                if(!aa_startTranscoding(nativeReference)) {
                    return false;
                }

                var size = GetImageTranscodedSize(imageIndex,levelIndex,format);
                byte[] data = new byte[size];

                bool result = false;
                fixed( void* dst = &(data[0]) ) {
                    result = aa_transcodeImage(nativeReference,dst,size,imageIndex,levelIndex,(uint)format,0,0);
                }
                transcodedData = data;
                return result;
            }
        }

        static bool initialized;
        static Dictionary<GraphicsFormat,BasisUniversal.TranscodeFormat> opaqueFormatDict;
        static Dictionary<GraphicsFormat,BasisUniversal.TranscodeFormat> alphaFormatDict;
        static Dictionary<TextureFormat,BasisUniversal.TranscodeFormat> opaqueFormatLegacyDict;
        static Dictionary<TextureFormat,BasisUniversal.TranscodeFormat> alphaFormatLegacyDict;

        public static void Init() {
            if(!initialized) {
                InitInternal();
            }
        }
        static void InitInternal()
        {
            initialized=true;
            aa_basis_init();

            if(opaqueFormatDict==null) {
                opaqueFormatDict = new Dictionary<GraphicsFormat, BasisUniversal.TranscodeFormat>();
                opaqueFormatDict.Add(GraphicsFormat.RGBA_BC7_SRGB,BasisUniversal.TranscodeFormat.BC7_M6_OPAQUE_ONLY);
                opaqueFormatDict.Add(GraphicsFormat.RGB_PVRTC_4Bpp_SRGB,BasisUniversal.TranscodeFormat.PVRTC1_4_OPAQUE_ONLY);
                opaqueFormatDict.Add(GraphicsFormat.RGB_ETC_UNorm,BasisUniversal.TranscodeFormat.ETC1);
                opaqueFormatDict.Add(GraphicsFormat.RGB_ETC2_SRGB,BasisUniversal.TranscodeFormat.ETC2);
                opaqueFormatDict.Add(GraphicsFormat.RGBA_DXT1_SRGB,BasisUniversal.TranscodeFormat.BC1);
                opaqueFormatDict.Add(GraphicsFormat.R_BC4_UNorm,BasisUniversal.TranscodeFormat.BC4);
                opaqueFormatDict.Add(GraphicsFormat.RG_BC5_UNorm,BasisUniversal.TranscodeFormat.BC5);
            }

            if(alphaFormatDict==null) {
                alphaFormatDict = new Dictionary<GraphicsFormat, BasisUniversal.TranscodeFormat>();
#if PLATFORM_IOS
                alphaFormatDict.Add(GraphicsFormat.RGBA_ETC2_SRGB,BasisUniversal.TranscodeFormat.ETC2);
#endif
                alphaFormatDict.Add(GraphicsFormat.RGBA_DXT5_SRGB,BasisUniversal.TranscodeFormat.BC3);
            }

            if(opaqueFormatLegacyDict==null) {
                opaqueFormatLegacyDict = new Dictionary<TextureFormat, BasisUniversal.TranscodeFormat>();
                opaqueFormatLegacyDict.Add(TextureFormat.BC7,BasisUniversal.TranscodeFormat.BC7_M6_OPAQUE_ONLY);
                opaqueFormatLegacyDict.Add(TextureFormat.PVRTC_RGB4,BasisUniversal.TranscodeFormat.PVRTC1_4_OPAQUE_ONLY);
                opaqueFormatLegacyDict.Add(TextureFormat.ETC_RGB4,BasisUniversal.TranscodeFormat.ETC1);
                opaqueFormatLegacyDict.Add(TextureFormat.ETC2_RGBA8,BasisUniversal.TranscodeFormat.ETC2);
                opaqueFormatLegacyDict.Add(TextureFormat.DXT1,BasisUniversal.TranscodeFormat.BC1);
                opaqueFormatLegacyDict.Add(TextureFormat.BC4,BasisUniversal.TranscodeFormat.BC4);
                opaqueFormatLegacyDict.Add(TextureFormat.BC5,BasisUniversal.TranscodeFormat.BC5);
            }

            if(alphaFormatLegacyDict==null) {
                alphaFormatLegacyDict = new Dictionary<TextureFormat, BasisUniversal.TranscodeFormat>();
                alphaFormatLegacyDict.Add(TextureFormat.DXT5,BasisUniversal.TranscodeFormat.BC3);
                alphaFormatLegacyDict.Add(TextureFormat.ETC2_RGBA8,BasisUniversal.TranscodeFormat.ETC2);            
                alphaFormatLegacyDict.Add(TextureFormat.ETC2_RGBA1,BasisUniversal.TranscodeFormat.ETC2); // Not sure if this works
            }

#if BASISU_VERBOSE
            CheckTextureSupport();
#endif
        }

        public static unsafe Texture2D LoadBytes( byte[] data ) {
            if(!initialized) {
                InitInternal();
            }
            
            Log("loading {0} bytes", data.Length);

            BasisTexture basis;
            fixed( void* src = &(data[0]) ) {
                basis = new BasisTexture(aa_create_basis(src,data.Length));
            }
            uint width;
            uint height;

            basis.GetImageSize(out width, out height);
            Log("image size {0} x {1}", width, height);

            bool hasAlpha = basis.GetHasAlpha();
            Log("image has alpha {0}",hasAlpha);

            var imageCount = basis.GetImageCount();
            Log("image count {0}",imageCount);

            for(uint i=0; i<imageCount;i++) {
                var levelCount = basis.GetLevelCount(i);
                Log("level count image {0}: {1}",i,levelCount);
            }

            GraphicsFormat gf;
            TextureFormat tf;
            TranscodeFormat transF = TranscodeFormat.ETC1;
            Texture2D texture = null;

            bool isPowerOfTwo = IsPowerOfTwo(width) && IsPowerOfTwo(height);
            bool isSquare = width==height;

            if(hasAlpha) {
                if(BasisUniversal.GetPreferredFormatAlpha(isPowerOfTwo,isSquare,out gf,out transF)) {
                    Log("Transcode to {0} (alpha)",gf);
                    texture = new Texture2D((int)width,(int)height,gf,TextureCreationFlags.None);
                } else
                if(BasisUniversal.GetPreferredFormatLegacyAlpha(isPowerOfTwo,isSquare,out tf,out transF)) {
                    Log("Transcode to {0} (legacy alpha)",tf);
                    texture = new Texture2D((int)width,(int)height,tf,false);
                }
            }
            
            if( !hasAlpha || texture==null ) {
                if(BasisUniversal.GetPreferredFormat(isPowerOfTwo,isSquare,out gf,out transF)) {
                    Log("Transcode to {0}",gf);
                    texture = new Texture2D((int)width,(int)height,gf,TextureCreationFlags.None);
                } else
                if(BasisUniversal.GetPreferredFormatLegacy(isPowerOfTwo,isSquare,out tf,out transF)) {
                    Log("Transcode to {0} (legacy)",tf);
                    texture = new Texture2D((int)width,(int)height,tf,false);
                }
            }

            if(texture==null) {
                Debug.LogError("No supported format found!\nRebuild with BASISU_VERBOSE scripting define to debug.");
                #if BASISU_VERBOSE
                BasisUniversal.CheckTextureSupport();
                #endif
                return null;
            }

            Log("Transcode to basisu {0}",transF);
            byte[] trData;
            if(basis.Transcode(0,0,transF,out trData)) {
                // Log("transcoded {0} bytes", trData.Length);
                texture.LoadRawTextureData(trData);
                texture.Apply();
                return texture;
            }
            return null;
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

        static bool IsPowerOfTwo(uint i) {
            return (i&(i-1))==0;
        }

        [System.Diagnostics.Conditional("BASISU_VERBOSE")]
        static void Log(string format, params object[] args) {
            Debug.LogFormat(format,args);
        }

        [DllImport(INTERFACE_DLL)]
        private static extern void aa_basis_init();

        [DllImport(INTERFACE_DLL)]
        private static unsafe extern System.IntPtr aa_create_basis( void * data, int length );

        [DllImport(INTERFACE_DLL)]
        private static extern void aa_delete_basis( IntPtr basis );

        [DllImport(INTERFACE_DLL)]
        private static extern bool aa_getHasAlpha( IntPtr basis );

        [DllImport(INTERFACE_DLL)]
        private static extern System.UInt32 aa_getNumImages( IntPtr basis );

        [DllImport(INTERFACE_DLL)]
        private static extern System.UInt32 aa_getNumLevels( IntPtr basis, System.UInt32 image_index);

        [DllImport(INTERFACE_DLL)]
        private static extern System.UInt32 aa_getImageWidth( IntPtr basis, System.UInt32 image_index, System.UInt32 level_index);

        [DllImport(INTERFACE_DLL)]
        private static extern System.UInt32 aa_getImageHeight( IntPtr basis, System.UInt32 image_index, System.UInt32 level_index);

        [DllImport(INTERFACE_DLL)]
        private static extern System.UInt32 aa_getImageTranscodedSizeInBytes( IntPtr basis, System.UInt32 image_index, System.UInt32 level_index, System.UInt32 format);

        [DllImport(INTERFACE_DLL)]
        private static extern bool aa_startTranscoding( IntPtr basis );

        [DllImport(INTERFACE_DLL)]
        private static unsafe extern bool aa_transcodeImage( IntPtr basis, void * dst, uint dst_size, System.UInt32 image_index, System.UInt32 level_index, System.UInt32 format, System.UInt32 pvrtc_wrap_addressing, System.UInt32 get_alpha_for_opaque_formats);

    }
}