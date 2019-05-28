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

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using UnityEngine;

public class BasisUniversal
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
    static Dictionary<TextureFormat,BasisUniversal.TranscodeFormat> opaqueFormatDict;
    static Dictionary<TextureFormat,BasisUniversal.TranscodeFormat> alphaFormatDict;

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
            opaqueFormatDict = new Dictionary<TextureFormat, BasisUniversal.TranscodeFormat>();
            opaqueFormatDict.Add(TextureFormat.BC7,BasisUniversal.TranscodeFormat.BC7_M6_OPAQUE_ONLY);
            opaqueFormatDict.Add(TextureFormat.PVRTC_RGB4,BasisUniversal.TranscodeFormat.PVRTC1_4_OPAQUE_ONLY);
            opaqueFormatDict.Add(TextureFormat.ETC_RGB4,BasisUniversal.TranscodeFormat.ETC1);
            opaqueFormatDict.Add(TextureFormat.ETC2_RGBA8,BasisUniversal.TranscodeFormat.ETC2);
            opaqueFormatDict.Add(TextureFormat.DXT1,BasisUniversal.TranscodeFormat.BC1);
            opaqueFormatDict.Add(TextureFormat.BC4,BasisUniversal.TranscodeFormat.BC4);
            opaqueFormatDict.Add(TextureFormat.BC5,BasisUniversal.TranscodeFormat.BC5);
        }

        if(alphaFormatDict==null) {
            alphaFormatDict = new Dictionary<TextureFormat, BasisUniversal.TranscodeFormat>();
            alphaFormatDict.Add(TextureFormat.DXT5,BasisUniversal.TranscodeFormat.BC3);
            alphaFormatDict.Add(TextureFormat.ETC2_RGBA8,BasisUniversal.TranscodeFormat.ETC2);            
            alphaFormatDict.Add(TextureFormat.ETC2_RGBA1,BasisUniversal.TranscodeFormat.ETC2); // Not sure if this works
        }
    }

    public static unsafe BasisTexture LoadBytes( byte[] data ) {
        if(!initialized) {
            InitInternal();
        }
        fixed( void* src = &(data[0]) ) {
            return new BasisTexture(aa_create_basis(src,data.Length));
        }
    }

    public static void CheckTextureSupport() {
        foreach(var format in opaqueFormatDict) {
            var supported = SystemInfo.SupportsTextureFormat(format.Key);
            Debug.LogFormat("TextureFormat {0} support: {1}",format.Key,supported);
        }
        foreach(var format in alphaFormatDict) {
            var supported = SystemInfo.SupportsTextureFormat(format.Key);
            Debug.LogFormat("TextureFormat (alpha) {0} support: {1}",format.Key,supported);
        }
    }

    public static bool GetPreferredFormat( out TextureFormat unityFormat, out TranscodeFormat transcodeFormat, bool hasAlpha = false ) {
        unityFormat = TextureFormat.DXT1;
        transcodeFormat = TranscodeFormat.BC1;
        
        var formats = hasAlpha
            ? alphaFormatDict
            : opaqueFormatDict;

        foreach(var format in formats) {
            var supported = SystemInfo.SupportsTextureFormat(format.Key);
            if (supported) {
                unityFormat = format.Key;
                transcodeFormat = format.Value;
                return true;
            }
        }
        if(hasAlpha) {
            // Fallback to opaque texture format
            var opaqueFound = GetPreferredFormat(out unityFormat,out transcodeFormat, false);
            if(opaqueFound) {
                Debug.LogWarningFormat("No supported alpha format found. Fallback to opaque format {0} ({1})",transcodeFormat,unityFormat);
            }
            return opaqueFound;
        }
        return false;
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
