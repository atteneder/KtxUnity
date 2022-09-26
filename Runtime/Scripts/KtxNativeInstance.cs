// Copyright (c) 2019-2022 Andreas Atteneder, All Rights Reserved.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
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
#elif UNITY_ANDROID || UNITY_STANDALONE || UNITY_WSA || UNITY_EDITOR || PLATFORM_LUMIN
        public const string INTERFACE_DLL = "ktx_unity";
#endif

        /// <summary>
        /// Benchmarks have shown that the 4 frame limit until disposal that
        /// Allocator.TempJob grants is sometimes not enough, so I chose Persistent.
        /// </summary>
        public const Allocator defaultAllocator = Allocator.Persistent;

        public IntPtr nativeReference;

        public bool valid => nativeReference != System.IntPtr.Zero;

        public KtxClassId ktxClass => ktx_get_classId(nativeReference);

        public bool needsTranscoding => ktxTexture2_NeedsTranscoding(nativeReference);

        public bool hasAlpha =>

            // Valid for KTX 2.0 Basis Universal only!
            // 1 = greyscale => no alpha
            // 2 = RRRA => alpha
            // 3 = RGB => no alpha
            // 4 = RGBA => alpha
            ktxTexture2_GetNumComponents(nativeReference) % 2 == 0;

        public bool isPowerOfTwo => LevelInfo.IsPowerOfTwo(baseWidth) && LevelInfo.IsPowerOfTwo(baseHeight);

        public bool isMultipleOfFour => LevelInfo.IsMultipleOfFour(baseWidth) && LevelInfo.IsMultipleOfFour(baseHeight);

        public bool isSquare => baseWidth==baseHeight;

        public uint baseWidth => ktx_get_baseWidth(nativeReference);

        public uint baseHeight => ktx_get_baseHeight(nativeReference);
        
        public uint baseDepth => ktx_get_baseDepth(nativeReference);

        public uint numLevels => ktx_get_numLevels(nativeReference);

        public bool isArray => ktx_get_isArray (nativeReference);

        public bool isCubemap => ktx_get_isCubemap (nativeReference);

        public bool isCompressed => ktx_get_isCompressed (nativeReference);

        public uint numDimensions => ktx_get_numDimensions (nativeReference);

        /// <summary>
        /// Specifies the number of array elements. If the texture is not an array texture, numLayers is 0.
        /// </summary>
        public uint numLayers => ktx_get_numLayers (nativeReference);

        /// <summary>
        /// faceCount specifies the number of cubemap faces.
        /// For cubemaps and cubemap arrays this is 6. For non cubemaps this is 1
        /// </summary>
        public uint numFaces => ktx_get_numFaces (nativeReference);

#if KTX_UNITY_GPU_UPLOAD
        /// <summary>
        /// Enqueues this texture for GPU upload in the KTX Native Unity Plugin
        /// </summary>
        internal void EnqueueForGpuUpload() {
            Profiler.BeginSample("EnqueueForGpuUpload");
            ktx_enqueue_upload(nativeReference);
            GL.IssuePluginEvent(GetRenderEventFunc(), 1);
            Profiler.EndSample();
        }

        /// <summary>
        /// Checks if this texture, that was previously equeued for GPU upload
        /// was successfully uploaded and creates a <see cref="Texture2D"/>
        /// from it.
        /// <param name="texture">Resulting texture or null, in case of errors</param>
        /// <param name="success">True if the texture was successfully created</param>
        /// <param name="graphicsFormat">Desired graphics format</param>
        /// <returns>True if the native plugin finished processing the texture, regardless
        /// of its success</returns>
        /// </summary>
        public bool TryCreateTexture(out Texture2D texture, out bool success, GraphicsFormat graphicsFormat) {
            Profiler.BeginSample("TryCreateTexture");
            if (ktx_dequeue_upload(nativeReference,out var nativeTexture, out var error)) {
                if (error == 0) {
                    var format = TranscodeFormatHelper.GetTextureFormat(graphicsFormat, out var linear);
                    texture = Texture2D.CreateExternalTexture(
                        (int)baseWidth,
                        (int)baseHeight,
                        format,
                        numLevels>1,
                        linear,
                        nativeTexture
                        );
                    success = true;
                }
                else {
                    texture = null;
                    success = false;
                }
                Profiler.EndSample();
                return true;
            }
            texture = null;
            success = false;
            Profiler.EndSample();
            return false;
        }
#endif // KTX_UNITY_GPU_UPLOAD

        public TextureOrientation orientation => (TextureOrientation) ktx_get_orientation(nativeReference);

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

        internal unsafe ErrorCode Load(NativeSlice<byte> data) {
            var src = data.GetUnsafeReadOnlyPtr();
            KtxErrorCode status;
            nativeReference = ktx_load_ktx(src, (uint)data.Length, out status);
            if(status!=KtxErrorCode.KTX_SUCCESS) {
#if DEBUG
                Debug.LogErrorFormat("KTX error code {0}",status);
#endif
                return ErrorCode.LoadingFailed;
            }
            return ErrorCode.Success;
        }

        public JobHandle LoadBytesJob(
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

        public unsafe Texture2D LoadTextureData(
            GraphicsFormat gf,
            uint layer = 0,
            uint mipLevel = 0,
            uint faceSlice = 0,
            bool mipChain = true
            ) 
        {
            
            Profiler.BeginSample("LoadTextureData");
            var levelCount = numLevels;
            var levelsNeeded = mipChain ? levelCount - mipLevel : 1;
            var mipmap = levelsNeeded>1;

            var width = baseWidth;
            var height = baseHeight;
            
            if (mipLevel > 0) {
                width = Math.Max(1u, width >> (int)mipLevel);
                height = Math.Max(1u, height >> (int)mipLevel);
            }
            
            Profiler.BeginSample("CreateTexture2D");
            var texture = new Texture2D(
                (int)width,
                (int)height,
                gf,
                mipmap ? TextureCreationFlags.MipChain : TextureCreationFlags.None
            );
            Profiler.EndSample();

            ktx_get_data(nativeReference,out var data,out var length);
            
            if(mipmap) {
                Profiler.BeginSample("MipMapCopy");

                for (var level = 0u; level < mipLevel; level++) {
                    length -= ktx_get_image_size(nativeReference, level);
                }
                
                var reorderedData = new NativeArray<byte>((int)length,Allocator.Temp);
                var reorderedDataPtr = reorderedData.GetUnsafePtr();
                var result = ktx_copy_data_levels_reverted(
                    nativeReference,
                    mipLevel,
                    layer,
                    faceSlice,
                    reorderedDataPtr,
                    (uint)reorderedData.Length
                );
                if (result != KtxErrorCode.KTX_SUCCESS) {
                    return texture;
                }
                Profiler.BeginSample("LoadRawTextureData");
                texture.LoadRawTextureData(reorderedData);
                Profiler.EndSample();
                reorderedData.Dispose();
                Profiler.EndSample();
            } else {
                Profiler.BeginSample("LoadRawTextureData");
                if (mipLevel > 0 || levelCount!=levelsNeeded || layer>0 || faceSlice>0) {
                    var result = ktx_get_image_offset(
                        nativeReference,
                        mipLevel,
                        layer,
                        faceSlice,
                        out var offset
                        );
                    if (result != KtxErrorCode.KTX_SUCCESS) {
                        return null;
                    }
                    data += offset;
                    length = ktx_get_image_size(nativeReference, mipLevel);
                }
                texture.LoadRawTextureData((IntPtr)data,(int)length);
                Profiler.EndSample();
            }
            texture.Apply(false,true);
            Profiler.EndSample();
            return texture;
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
        unsafe static extern System.IntPtr ktx_load_ktx(void * data, uint length, out KtxErrorCode status);

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_baseWidth ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_baseHeight ( System.IntPtr ktxTexture );
        
        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_baseDepth ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern bool ktxTexture2_NeedsTranscoding( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint ktxTexture2_GetNumComponents( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        public static extern KtxErrorCode ktxTexture2_TranscodeBasis(System.IntPtr ktxTexture, TranscodeFormat outputFormat, uint transcodeFlags);

        [DllImport(INTERFACE_DLL)]
        unsafe static extern void ktx_get_data(System.IntPtr ktxTexture, out byte* data, out uint length);
        
        [DllImport(INTERFACE_DLL)]
        unsafe static extern KtxErrorCode ktx_copy_data_levels_reverted(
            IntPtr ktxTexture,
            uint startLevel,
            uint layer,
            uint faceSlice,
            void* destination,
            uint destinationLength
            );

        [DllImport(INTERFACE_DLL)]
        static extern void ktx_unload_ktx(System.IntPtr ktxTexture);

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_numLevels ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_orientation ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern KtxClassId ktx_get_classId ( System.IntPtr ktxTexture );

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

        /*
        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_vkFormat ( System.IntPtr ktxTexture );

        [DllImport(INTERFACE_DLL)]
        static extern KtxSupercmpScheme ktx_get_supercompressionScheme ( System.IntPtr ktxTexture );
        //*/
        
        [DllImport(INTERFACE_DLL)]
        static extern KtxErrorCode ktx_get_image_offset(
            IntPtr ktxTexture,
            uint level,
            uint layer,
            uint faceSlice,
            out int pOffset
            );
        
        [DllImport(INTERFACE_DLL)]
        static extern uint ktx_get_image_size(
            IntPtr ktxTexture,
            uint level
        );

#if KTX_UNITY_GPU_UPLOAD
        [DllImport(INTERFACE_DLL)]
        static extern void ktx_enqueue_upload(IntPtr ktx);
        
        [DllImport(INTERFACE_DLL)]
        static extern bool ktx_dequeue_upload(IntPtr ktx, out IntPtr texture, out uint error);

        [DllImport(INTERFACE_DLL)]
        static extern IntPtr GetRenderEventFunc();
#endif
    }
} 
