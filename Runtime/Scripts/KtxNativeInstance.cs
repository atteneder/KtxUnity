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
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using IntPtr=System.IntPtr;

namespace BasisUniversalUnity {

    /*
    struct KtxOrientation {
        public KtxOrientationX x;
        public KtxOrientationY y;
        public KtxOrientationY z;
    }
    //*/

    public class KtxNativeInstance : IMetaData, ILevelInfo
    {
#if UNITY_EDITOR_OSX || UNITY_WEBGL || UNITY_IOS
        public const string INTERFACE_DLL = "__Internal";
#elif UNITY_ANDROID || UNITY_STANDALONE
        public const string INTERFACE_DLL = "ktx_unity";
#endif

        public IntPtr nativeReference;

        public bool hasAlpha {
            get {
                return aa_ktx_get_has_alpha(nativeReference);
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
                return aa_ktx_get_baseWidth(nativeReference);
            }
        }

        public uint baseHeight {
            get {
                return aa_ktx_get_baseHeight(nativeReference);
            }
        }

        /*
        KtxClassId classId {
            get {
                return aa_ktx_get_classId(nativeReference);
            }
        }
        bool isArray {
            get {
                return aa_ktx_get_isArray(nativeReference);
            }
        }
        bool isCubemap {
            get {
                return aa_ktx_get_isCubemap(nativeReference);
            }
        }
        bool isCompressed {
            get {
                return aa_ktx_get_isCompressed(nativeReference);
            }
        }
        uint numDimensions {
            get {
                return aa_ktx_get_numDimensions(nativeReference);
            }
        }
        uint numLevels {
            get {
                return aa_ktx_get_numLevels(nativeReference);
            }
        }
        uint numLayers {
            get {
                return aa_ktx_get_numLayers(nativeReference);
            }
        }
        uint numFaces {
            get {
                return aa_ktx_get_numFaces(nativeReference);
            }
        }
        uint vkFormat {
            get {
                return aa_ktx_get_vkFormat(nativeReference);
            }
        }
        KtxSupercmpScheme supercompressionScheme {
            get {
                return aa_ktx_get_supercompressionScheme(nativeReference);
            }
        }
        KtxOrientation orientation {
            get {
                KtxOrientation orientation;
                aa_ktx_get_orientation(nativeReference,out orientation);
                return orientation;
            }
        }
        //*/

        public unsafe bool Load(NativeArray<byte> data) {
            var src = NativeArrayUnsafeUtility.GetUnsafePtr(data);
            KtxErrorCode status;
            nativeReference = aa_load_ktx(src, data.Length, out status);
            if(status!=KtxErrorCode.KTX_SUCCESS) {
                Debug.LogErrorFormat("KTX error code {0}",status);
                return false;
            }
            return true;
        }

        public unsafe void LoadRawTextureData(Texture2D texture) {
            System.IntPtr data;
            uint length;
            aa_ktx_get_data(nativeReference,out data,out length);
            texture.LoadRawTextureData(data,(int)length);
        }

        public unsafe JobHandle LoadBytesJob(
            ref KtxTranscodeJob job,
            TranscodeFormat transF
        ) {
            UnityEngine.Profiling.Profiler.BeginSample("Ktx.LoadBytesJob");

            job.result = new NativeArray<KtxErrorCode>(1,BasisUniversal.defaultAllocator);
            job.nativeReference = nativeReference;
            job.outputFormat = transF;

            var jobHandle = job.Schedule();

            UnityEngine.Profiling.Profiler.EndSample();
            return jobHandle;
        }

        [DllImport(INTERFACE_DLL)]
        unsafe static extern System.IntPtr aa_load_ktx(void * data, int length, out KtxErrorCode status);

        [DllImport(INTERFACE_DLL)]
        static extern uint aa_ktx_get_baseWidth ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint aa_ktx_get_baseHeight ( System.IntPtr ktxTexture );
        [DllImport(INTERFACE_DLL)]
         static extern bool aa_ktx_get_has_alpha( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        public static extern KtxErrorCode aa_transcode_ktx(System.IntPtr ktxTexture, TranscodeFormat outputFormat, uint transcodeFlags);

        [DllImport(INTERFACE_DLL)]
        unsafe static extern void aa_ktx_get_data(System.IntPtr ktxTexture, out System.IntPtr data, out uint length);

        /*
        [DllImport(INTERFACE_DLL)]
        static extern KtxClassId aa_ktx_get_classId ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern bool aa_ktx_get_isArray ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern bool aa_ktx_get_isCubemap ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern bool aa_ktx_get_isCompressed ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint aa_ktx_get_numDimensions ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint aa_ktx_get_numLevels ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint aa_ktx_get_numLayers ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint aa_ktx_get_numFaces ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint aa_ktx_get_vkFormat ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern KtxSupercmpScheme aa_ktx_get_supercompressionScheme ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern void aa_ktx_get_orientation ( System.IntPtr ktxTexture, out KtxOrientation x );

        [DllImport(INTERFACE_DLL)]
        static extern int aa_unload_ktx(System.IntPtr ktxTexture);
        //*/
    }
} 
