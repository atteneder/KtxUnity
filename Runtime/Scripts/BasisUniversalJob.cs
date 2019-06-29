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
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using IntPtr = System.IntPtr;

namespace BasisUniversalUnity {

    public unsafe struct BasisUniversalJob : IJob
    {
        [WriteOnly]
        public NativeArray<bool> result;

        [ReadOnly]
        public TranscodeFormat format;

        [ReadOnly]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr nativeReference;

        [ReadOnly]
        public uint size;

        [ReadOnly]
        public uint imageIndex;

        [ReadOnly]
        public uint levelIndex;

        [WriteOnly]
        public NativeArray<byte> textureData;

        public void Execute()
        {
            bool success = aa_startTranscoding(nativeReference);
            void* textureDataPtr = NativeArrayUnsafeUtility.GetUnsafePtr<byte>(textureData);
            success = success && aa_transcodeImage(nativeReference,textureDataPtr,size,imageIndex,levelIndex,(uint)format,0,0);
            result[0] = success;
        }

        [DllImport(BasisUniversal.INTERFACE_DLL)]
        private static extern bool aa_startTranscoding( IntPtr basis );

        [DllImport(BasisUniversal.INTERFACE_DLL)]
        private static unsafe extern bool aa_transcodeImage( IntPtr basis, void * dst, uint dst_size, System.UInt32 image_index, System.UInt32 level_index, System.UInt32 format, System.UInt32 pvrtc_wrap_addressing, System.UInt32 get_alpha_for_opaque_formats);
    }
}
