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
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using IntPtr=System.IntPtr;

namespace KtxUnity {

    public class KtxNativeInstance : IMetaData, ILevelInfo
    {
#if UNITY_EDITOR_OSX || UNITY_WEBGL || UNITY_IOS
        public const string INTERFACE_DLL = "__Internal";
#elif UNITY_ANDROID || UNITY_STANDALONE || UNITY_WSA || UNITY_EDITOR
        public const string INTERFACE_DLL = "ktx_unity";
#endif

        /// <summary>
        /// Benchmarks have shown that the 4 frame limit until disposal that
        /// Allocator.TempJob grants is sometimes not enough, so I chose Persistent.
        /// </summary>
        public const Allocator defaultAllocator = Allocator.Persistent;

        public IntPtr nativeReference;

        public KtxNativeInstance(NativeSlice<byte> data) {
            Load(data);
        }

        public bool valid {
            get {
                return nativeReference != System.IntPtr.Zero;
            }
        }

        public KtxClassId ktxClass {
            get {
                return ktx_get_classId(nativeReference);
            }
        }

        public bool needsTranscoding {
            get {
                return ktxTexture2_NeedsTranscoding(nativeReference);
            }
        }

        public bool hasAlpha {
            get {
                // Valid for KTX 2.0 Basis Universal only!
                // 1 = greyscale => no alpha
                // 2 = RRRA => alpha
                // 3 = RGB => no alpha
                // 4 = RGBA => alpha
                return ktxTexture2_GetNumComponents(nativeReference) % 2 == 0;
            }
        }

        public bool isPowerOfTwo {
            get {
                return LevelInfo.IsPowerOfTwo(baseWidth) && LevelInfo.IsPowerOfTwo(baseHeight);
            }
        }

        public bool isSquare {
            get {
                return baseWidth==baseHeight;
            }
        }

        public uint baseWidth {
            get {
                return ktx_get_baseWidth(nativeReference);
            }
        }

        public uint baseHeight {
            get {
                return ktx_get_baseHeight(nativeReference);
            }
        }
        public uint numLevels {
            get {
                return ktx_get_numLevels(nativeReference);
            }
        }

        public TextureOrientation orientation {
            get {
                return (TextureOrientation) ktx_get_orientation(nativeReference);;
            }
        }

        /*
        KtxClassId classId {
            get {
                return ktx_get_classId(nativeReference);
            }
        }
        bool isArray {
            get {
                return ktx_get_isArray(nativeReference);
            }
        }
        bool isCubemap {
            get {
                return ktx_get_isCubemap(nativeReference);
            }
        }
        bool isCompressed {
            get {
                return ktx_get_isCompressed(nativeReference);
            }
        }
        uint numDimensions {
            get {
                return ktx_get_numDimensions(nativeReference);
            }
        }
        
        uint numLayers {
            get {
                return ktx_get_numLayers(nativeReference);
            }
        }
        uint numFaces {
            get {
                return ktx_get_numFaces(nativeReference);
            }
        }
        uint vkFormat {
            get {
                return ktx_get_vkFormat(nativeReference);
            }
        }
        KtxSupercmpScheme supercompressionScheme {
            get {
                return ktx_get_supercompressionScheme(nativeReference);
            }
        }
        //*/

        unsafe bool Load(NativeSlice<byte> data) {
            var src = data.GetUnsafeReadOnlyPtr();
            KtxErrorCode status;
            nativeReference = ktx_load_ktx(src, data.Length, out status);
            if(status!=KtxErrorCode.KTX_SUCCESS) {
                Debug.LogErrorFormat("KTX error code {0}",status);
                return false;
            }
            return true;
        }

        public unsafe Texture2D LoadTextureData(GraphicsFormat gf) {
            Profiler.BeginSample("LoadTextureData");
            byte* data;
            uint length;
            ktx_get_data(nativeReference,out data,out length);
            bool mipmap = numLevels>1;

            Profiler.BeginSample("CreateTexture2D");
            var texture = new Texture2D(
                (int)baseWidth,
                (int)baseHeight,
                gf,
                mipmap ? TextureCreationFlags.MipChain : TextureCreationFlags.None
                );
            Profiler.EndSample();

            if(mipmap) {
                Profiler.BeginSample("MipMapCopy");
                var reorderedData = new NativeArray<byte>((int)length,Allocator.Temp);
                void * reorderedDataPtr = NativeArrayUnsafeUtility.GetUnsafePtr<byte>(reorderedData);
                ktx_copy_data_levels_reverted(
                    nativeReference,
                    reorderedDataPtr,
                    (uint)reorderedData.Length
                    );
                texture.LoadRawTextureData(reorderedData);
                reorderedData.Dispose();
                Profiler.EndSample();
            } else {
                Profiler.BeginSample("LoadRawTextureData");
                texture.LoadRawTextureData((IntPtr)data,(int)length);
                Profiler.EndSample();
            }
            texture.Apply(false,true);
            Profiler.EndSample();
            return texture;
        }

        public unsafe JobHandle LoadBytesJob(
            ref KtxTranscodeJob job,
            TranscodeFormat transF
        ) {
            UnityEngine.Profiling.Profiler.BeginSample("Ktx.LoadBytesJob");

            job.result = new NativeArray<KtxErrorCode>(1,defaultAllocator);
            job.nativeReference = nativeReference;
            job.outputFormat = transF;

            var jobHandle = job.Schedule();

            UnityEngine.Profiling.Profiler.EndSample();
            return jobHandle;
        }

        /// <summary>
        /// Removes the native KTX object and frees up the memory
        /// </summary>
        public void Unload() {
            if(valid) {
                ktx_unload_ktx(nativeReference);
                nativeReference = IntPtr.Zero;
            }
        }

        ~KtxNativeInstance() {
            Unload();
        }

        [DllImport(INTERFACE_DLL)]
        unsafe static extern System.IntPtr ktx_load_ktx(void * data, int length, out KtxErrorCode status);

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_baseWidth ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_baseHeight ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern bool ktxTexture2_NeedsTranscoding( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern int ktxTexture2_GetNumComponents( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        public static extern KtxErrorCode ktx_transcode_ktx(System.IntPtr ktxTexture, TranscodeFormat outputFormat, uint transcodeFlags);

        [DllImport(INTERFACE_DLL)]
        unsafe static extern void ktx_get_data(System.IntPtr ktxTexture, out byte* data, out uint length);
        [DllImport(INTERFACE_DLL)]
        unsafe static extern void ktx_copy_data_levels_reverted(System.IntPtr ktxTexture, void* destination, uint destinationLength);

        [DllImport(INTERFACE_DLL)]
        static extern void ktx_unload_ktx(System.IntPtr ktxTexture);

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_numLevels ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_orientation ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern KtxClassId ktx_get_classId ( System.IntPtr ktxTexture );

        /*
        [DllImport(INTERFACE_DLL)]
        static extern bool ktx_get_isArray ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern bool ktx_get_isCubemap ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern bool ktx_get_isCompressed ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_numDimensions ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_numLayers ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_numFaces ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_vkFormat ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern KtxSupercmpScheme ktx_get_supercompressionScheme ( System.IntPtr ktxTexture );
        //*/
    }
} 
