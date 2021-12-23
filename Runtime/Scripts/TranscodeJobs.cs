﻿// Copyright (c) 2019-2021 Andreas Atteneder, All Rights Reserved.

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
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using IntPtr = System.IntPtr;

namespace KtxUnity {

    public unsafe struct BasisUniversalJob : IJob
    {
        [WriteOnly]
        public NativeArray<bool> result;

        [ReadOnly]
        public TranscodeFormat format;

        [ReadOnly]
        public bool mipChain;

        [ReadOnly]
        public uint mipLevel;


        [ReadOnly]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr nativeReference;

        [ReadOnly]
        public NativeArray<uint> sizes;

        [ReadOnly]
        public NativeArray<uint> offsets;

        [ReadOnly]
        public uint imageIndex;

        [WriteOnly]
        public NativeArray<byte> textureData;

        [WriteOnly]
        public NativeArray<byte> textureDataAlpha;

        public void Execute()
        {
            bool success = ktx_basisu_startTranscoding(nativeReference);
            void* textureDataPtr = NativeArrayUnsafeUtility.GetUnsafePtr<byte>(textureData);
            void* textureDataAlphaPtr = NativeArrayUnsafeUtility.GetUnsafePtr<byte>(textureDataAlpha);
            bool DoAlpha = false;
            if (textureDataAlpha.Length > 0)
            {
                DoAlpha = true;
            }
            for (uint i = 0; i < offsets.Length; i++)
            {
                success = success &&
                ktx_basisu_transcodeImage(
                    nativeReference,
                    (byte*)textureDataPtr+offsets[(int)i],
                    sizes[(int)i],
                    imageIndex,
                    mipLevel+i,
                    (uint)format,
                    0,
                    0
                    );
                if(!success) break;
                if (DoAlpha)
                {
                    ktx_basisu_transcodeImage(
                        nativeReference,
                        (byte*)textureDataAlphaPtr + offsets[(int)i],
                        sizes[(int)i],
                        imageIndex,
                        mipLevel + i,
                        (uint)format,
                        0,
                        1
                        );
                }
            }
            result[0] = success;
        }

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern bool ktx_basisu_startTranscoding( IntPtr basis );

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static unsafe extern bool ktx_basisu_transcodeImage( IntPtr basis, void * dst, uint dst_size, System.UInt32 image_index, System.UInt32 level_index, System.UInt32 format, System.UInt32 pvrtc_wrap_addressing, System.UInt32 get_alpha_for_opaque_formats);
    }

    public unsafe struct KtxTranscodeJob : IJob {

        [WriteOnly]
        public NativeArray<KtxErrorCode> result;

        [ReadOnly]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr nativeReference;

        [ReadOnly]
        public TranscodeFormat outputFormat;

        public void Execute() {
            result[0] = KtxNativeInstance.ktxTexture2_TranscodeBasis(
                nativeReference,
                outputFormat,
                0 // transcodeFlags
                );
        }
    }
}
