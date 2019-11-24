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

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Experimental.Rendering;
using Unity.Jobs;
using Unity.Collections;

namespace KtxUnity {

    public static class BasisUniversal
    {
        static bool initialized;
        static int transcoderCountAvailable = 8;
        

#if POOL_TRANSCODERS
        static Stack<TranscoderInstance> transcoderPool;
#endif

        static void InitInternal()
        {
            initialized=true;
            TranscodeFormatHelper.Init();
            ktx_basisu_basis_init();
            transcoderCountAvailable = UnityEngine.SystemInfo.processorCount;
        }
        
        public static BasisUniversalTranscoderInstance GetTranscoderInstance() {
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
                return new BasisUniversalTranscoderInstance(ktx_basisu_create_basis());
            } else {
                return null;
            }
        }

        public static void ReturnTranscoderInstance( BasisUniversalTranscoderInstance transcoder ) {
#if POOL_TRANSCODERS
            if(transcoderPool==null) {
                transcoderPool = new Stack<TranscoderInstance>();
            }
            transcoderPool.Push(transcoder);
#endif
            transcoderCountAvailable++;
        }

        public unsafe static JobHandle LoadBytesJob(
            ref BasisUniversalJob job,
            BasisUniversalTranscoderInstance basis,
            NativeSlice<byte> basisuData,
            TranscodeFormat transF
        ) {
            
            Profiler.BeginSample("BasisU.LoadBytesJob");
            
            var size = basis.GetImageTranscodedSize(0,0,transF);

            job.format = transF;
            job.size = size;
            job.nativeReference = basis.nativeReference;
            
            job.textureData = new NativeArray<byte>((int)size,KtxNativeInstance.defaultAllocator);

            var jobHandle = job.Schedule();

            Profiler.EndSample();
            return jobHandle;
        }

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern void ktx_basisu_basis_init();

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static unsafe extern System.IntPtr ktx_basisu_create_basis();
    }
}