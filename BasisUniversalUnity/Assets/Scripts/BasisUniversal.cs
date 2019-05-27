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

    static void Init()
    {
        initialized=true;
        aa_basis_init();
    }

    public static unsafe BasisTexture LoadBytes( byte[] data ) {
        if(!initialized) {
            Init();
        }
        fixed( void* src = &(data[0]) ) {
            return new BasisTexture(aa_create_basis(src,data.Length));
        }
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
