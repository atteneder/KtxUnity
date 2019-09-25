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

namespace BasisUniversalUnity {

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
            aa_basis_init();
            transcoderCountAvailable = UnityEngine.SystemInfo.processorCount;
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

            bool match = TranscodeFormatHelper.GetFormatsForImage(meta,li,out graphicsFormat,out textureFormat,out transF);
            
            if(!match) {
                Debug.LogError("No supported format found!\nRebuild with BASISU_VERBOSE scripting define to debug.");
                #if BASISU_VERBOSE
                TranscodeFormatHelper.CheckTextureSupport();
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
            
            job.textureData = new NativeArray<byte>((int)size,KtxNativeInstance.defaultAllocator);

            var jobHandle = job.Schedule();

            Profiler.EndSample();
            return jobHandle;
        }

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static extern void aa_basis_init();

        [DllImport(KtxNativeInstance.INTERFACE_DLL)]
        private static unsafe extern System.IntPtr aa_create_basis();
    }
}