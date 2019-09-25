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


using System.Runtime.InteropServices;
using System;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;


namespace BasisUniversalUnity {

    public class TranscoderInstance {
        public IntPtr nativeReference;

        public TranscoderInstance( IntPtr nativeReference ) {
            this.nativeReference = nativeReference;
        }

        public unsafe bool Open(NativeArray<byte> data) {
            void* src = NativeArrayUnsafeUtility.GetUnsafePtr(data);
            bool success = aa_open_basis(nativeReference,src,data.Length);
            if(!success) {
                Debug.LogError("Couldn't validate BasisU header!");
            }
            return success;
        }

        public unsafe bool Open( void* src, int size ) {
            bool success = aa_open_basis(nativeReference,src,size);
            if(!success) {
                Debug.LogError("Couldn't validate BasisU header!");
            }
            return success;
        }

        public MetaData LoadMetaData() {
            Profiler.BeginSample("LoadMetaData");
            MetaData meta = new MetaData();
            meta.hasAlpha = GetHasAlpha();
            var imageCount = GetImageCount();
            meta.images = new ImageInfo[imageCount];
            for(uint i=0; i<imageCount;i++) {
                var ii = new ImageInfo();
                var levelCount = GetLevelCount(i);
                ii.levels = new LevelInfo[levelCount];
                for (uint l = 0; l < levelCount; l++)
                {
                    var li = new LevelInfo();
                    GetImageSize(out li.width, out li.height,i,l);
                    ii.levels[l] = li;
                }
                meta.images[i] = ii;
            }
            Profiler.EndSample();
            return meta;
        }

        public void Close() {
            aa_close_basis(nativeReference);
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

        public uint GetImageTranscodedSize(uint imageIndex, uint levelIndex, TranscodeFormat format) {
            return aa_getImageTranscodedSizeInBytes(nativeReference,imageIndex,levelIndex,(uint)format);
        }

        public unsafe bool Transcode(uint imageIndex,uint levelIndex,TranscodeFormat format,out byte[] transcodedData ) {
            Profiler.BeginSample("BasisU.Transcode");
            transcodedData = null;

            if(!aa_startTranscoding(nativeReference)) {
                Profiler.EndSample();
                return false;
            }

            var size = GetImageTranscodedSize(imageIndex,levelIndex,format);
            byte[] data = new byte[size];

            bool result = false;
            fixed( void* dst = &(data[0]) ) {
                result = aa_transcodeImage(nativeReference,dst,size,imageIndex,levelIndex,(uint)format,0,0);
            }
            transcodedData = data;
            Profiler.EndSample();
            return result;
        }

        ~TranscoderInstance() {
            aa_delete_basis(nativeReference);
        }

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static unsafe extern bool aa_open_basis( IntPtr basis, void * data, int length );

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static unsafe extern void aa_close_basis( IntPtr basis );

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern void aa_delete_basis( IntPtr basis );

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern bool aa_getHasAlpha( IntPtr basis );

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern System.UInt32 aa_getNumImages( IntPtr basis );

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern System.UInt32 aa_getNumLevels( IntPtr basis, System.UInt32 image_index);

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern System.UInt32 aa_getImageWidth( IntPtr basis, System.UInt32 image_index, System.UInt32 level_index);

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern System.UInt32 aa_getImageHeight( IntPtr basis, System.UInt32 image_index, System.UInt32 level_index);

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern System.UInt32 aa_getImageTranscodedSizeInBytes( IntPtr basis, System.UInt32 image_index, System.UInt32 level_index, System.UInt32 format);

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern bool aa_startTranscoding( IntPtr basis );

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static unsafe extern bool aa_transcodeImage( IntPtr basis, void * dst, uint dst_size, System.UInt32 image_index, System.UInt32 level_index, System.UInt32 format, System.UInt32 pvrtc_wrap_addressing, System.UInt32 get_alpha_for_opaque_formats);
    }
}