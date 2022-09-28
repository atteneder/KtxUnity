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
#if UNITY_EDITOR_OSX || UNITY_WEBGL || (UNITY_IOS && !UNITY_EDITOR)
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

        public GraphicsFormat graphicsFormat => GetGraphicsFormat(ktx_get_vkFormat(nativeReference));

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
                    texture = Texture2D.CreateExternalTexture(
                        (int)baseWidth,
                        (int)baseHeight,
                        GraphicsFormatUtility.GetTextureFormat(graphicsFormat),
                        numLevels>1,
                        !GraphicsFormatUtility.IsSRGBFormat(graphicsFormat),
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

        static GraphicsFormat GetGraphicsFormat(VkFormat vkFormat) {
            switch (vkFormat) {
                case VkFormat.Astc8X8SrgbBlock: return GraphicsFormat.RGBA_ASTC8X8_SRGB;
                case VkFormat.Astc8X8UNormBlock: return GraphicsFormat.RGBA_ASTC8X8_UNorm;
                case VkFormat.B10G11R11UFloatPack32: return GraphicsFormat.B10G11R11_UFloatPack32;
                case VkFormat.BC2SrgbBlock: return GraphicsFormat.RGBA_DXT3_SRGB;
                case VkFormat.BC2UNormBlock: return GraphicsFormat.RGBA_DXT3_UNorm;
                case VkFormat.BC3SrgbBlock: return GraphicsFormat.RGBA_DXT5_SRGB;
                case VkFormat.BC3UNormBlock: return GraphicsFormat.RGBA_DXT5_UNorm;
                case VkFormat.ETC2R8G8B8UNormBlock: return GraphicsFormat.RGB_ETC2_UNorm;
                case VkFormat.R16SFloat: return GraphicsFormat.R16_SFloat;
                case VkFormat.R16SInt: return GraphicsFormat.R16_SInt;
                case VkFormat.R16UInt: return GraphicsFormat.R16_UInt;
                case VkFormat.R32SFloat: return GraphicsFormat.R32_SFloat;
                case VkFormat.R8G8B8A8SInt: return GraphicsFormat.R8G8B8A8_SInt;
                case VkFormat.R8G8B8A8SNorm: return GraphicsFormat.R8G8B8A8_SNorm;
                case VkFormat.R8G8B8A8Srgb: return GraphicsFormat.R8G8B8A8_SRGB;
                case VkFormat.R8G8B8A8UInt: return GraphicsFormat.R8G8B8A8_UInt;
                case VkFormat.R8G8B8A8UNorm: return GraphicsFormat.R8G8B8A8_UNorm;
                case VkFormat.R8G8B8Srgb: return GraphicsFormat.R8G8B8_SRGB;
                case VkFormat.R8G8B8UNorm: return GraphicsFormat.R8G8B8_UNorm;
                case VkFormat.R8G8UNorm: return GraphicsFormat.R8G8_UNorm;
                case VkFormat.R8UInt: return GraphicsFormat.R8_UInt;
                case VkFormat.R8UNorm: return GraphicsFormat.R8_UNorm;
                case VkFormat.A1R5G5B5UNormPack16:
                case VkFormat.A2B10G10R10SIntPack32:
                case VkFormat.A2B10G10R10SNormPack32:
                case VkFormat.A2B10G10R10SScaledPack32:
                case VkFormat.A2B10G10R10UIntPack32:
                case VkFormat.A2B10G10R10UNormPack32:
                case VkFormat.A2B10G10R10UScaledPack32:
                case VkFormat.A2R10G10B10SIntPack32:
                case VkFormat.A2R10G10B10SNormPack32:
                case VkFormat.A2R10G10B10SScaledPack32:
                case VkFormat.A2R10G10B10UIntPack32:
                case VkFormat.A2R10G10B10UNormPack32:
                case VkFormat.A2R10G10B10UScaledPack32:
                case VkFormat.A4B4G4R4UNormPack16Ext:
                case VkFormat.A4R4G4B4UNormPack16Ext:
                case VkFormat.A8B8G8R8SIntPack32:
                case VkFormat.A8B8G8R8SNormPack32:
                case VkFormat.A8B8G8R8SScaledPack32:
                case VkFormat.A8B8G8R8SrgbPack32:
                case VkFormat.A8B8G8R8UIntPack32:
                case VkFormat.A8B8G8R8UNormPack32:
                case VkFormat.A8B8G8R8UScaledPack32:
                case VkFormat.Astc10X10SFloatBlockExt:
                case VkFormat.Astc10X10SrgbBlock:
                case VkFormat.Astc10X10UNormBlock:
                case VkFormat.Astc10X5SFloatBlockExt:
                case VkFormat.Astc10X5SrgbBlock:
                case VkFormat.Astc10X5UNormBlock:
                case VkFormat.Astc10X6SFloatBlockExt:
                case VkFormat.Astc10X6SrgbBlock:
                case VkFormat.Astc10X6UNormBlock:
                case VkFormat.Astc10X8SFloatBlockExt:
                case VkFormat.Astc10X8SrgbBlock:
                case VkFormat.Astc10X8UNormBlock:
                case VkFormat.Astc12X10SFloatBlockExt:
                case VkFormat.Astc12X10SrgbBlock:
                case VkFormat.Astc12X10UNormBlock:
                case VkFormat.Astc12X12SFloatBlockExt:
                case VkFormat.Astc12X12SrgbBlock:
                case VkFormat.Astc12X12UNormBlock:
                case VkFormat.Astc3X3X3SFloatBlockExt:
                case VkFormat.Astc3X3X3SrgbBlockExt:
                case VkFormat.Astc3X3X3UNormBlockExt:
                case VkFormat.Astc4X3X3SFloatBlockExt:
                case VkFormat.Astc4X3X3SrgbBlockExt:
                case VkFormat.Astc4X3X3UNormBlockExt:
                case VkFormat.Astc4X4SFloatBlockExt:
                case VkFormat.Astc4X4SrgbBlock:
                case VkFormat.Astc4X4UNormBlock:
                case VkFormat.Astc4X4X3SFloatBlockExt:
                case VkFormat.Astc4X4X3SrgbBlockExt:
                case VkFormat.Astc4X4X3UNormBlockExt:
                case VkFormat.Astc4X4X4SFloatBlockExt:
                case VkFormat.Astc4X4X4SrgbBlockExt:
                case VkFormat.Astc4X4X4UNormBlockExt:
                case VkFormat.Astc5X4SFloatBlockExt:
                case VkFormat.Astc5X4SrgbBlock:
                case VkFormat.Astc5X4UNormBlock:
                case VkFormat.Astc5X4X4SFloatBlockExt:
                case VkFormat.Astc5X4X4SrgbBlockExt:
                case VkFormat.Astc5X4X4UNormBlockExt:
                case VkFormat.Astc5X5SFloatBlockExt:
                case VkFormat.Astc5X5SrgbBlock:
                case VkFormat.Astc5X5UNormBlock:
                case VkFormat.Astc5X5X4SFloatBlockExt:
                case VkFormat.Astc5X5X4SrgbBlockExt:
                case VkFormat.Astc5X5X4UNormBlockExt:
                case VkFormat.Astc5X5X5SFloatBlockExt:
                case VkFormat.Astc5X5X5SrgbBlockExt:
                case VkFormat.Astc5X5X5UNormBlockExt:
                case VkFormat.Astc6X5SFloatBlockExt:
                case VkFormat.Astc6X5SrgbBlock:
                case VkFormat.Astc6X5UNormBlock:
                case VkFormat.Astc6X5X5SFloatBlockExt:
                case VkFormat.Astc6X5X5SrgbBlockExt:
                case VkFormat.Astc6X5X5UNormBlockExt:
                case VkFormat.Astc6X6SFloatBlockExt:
                case VkFormat.Astc6X6SrgbBlock:
                case VkFormat.Astc6X6UNormBlock:
                case VkFormat.Astc6X6X5SFloatBlockExt:
                case VkFormat.Astc6X6X5SrgbBlockExt:
                case VkFormat.Astc6X6X5UNormBlockExt:
                case VkFormat.Astc6X6X6SFloatBlockExt:
                case VkFormat.Astc6X6X6SrgbBlockExt:
                case VkFormat.Astc6X6X6UNormBlockExt:
                case VkFormat.Astc8X5SFloatBlockExt:
                case VkFormat.Astc8X5SrgbBlock:
                case VkFormat.Astc8X5UNormBlock:
                case VkFormat.Astc8X6SFloatBlockExt:
                case VkFormat.Astc8X6SrgbBlock:
                case VkFormat.Astc8X6UNormBlock:
                case VkFormat.Astc8X8SFloatBlockExt:
                case VkFormat.B10X6G10X6R10X6G10X6422UNorm4Pack16:
                case VkFormat.B12X4G12X4R12X4G12X4422UNorm4Pack16:
                case VkFormat.B16G16R16G16422UNorm:
                case VkFormat.B4G4R4A4UNormPack16:
                case VkFormat.B5G5R5A1UNormPack16:
                case VkFormat.B5G6R5UNormPack16:
                case VkFormat.B8G8R8A8SInt:
                case VkFormat.B8G8R8A8SNorm:
                case VkFormat.B8G8R8A8SScaled:
                case VkFormat.B8G8R8A8Srgb:
                case VkFormat.B8G8R8A8UInt:
                case VkFormat.B8G8R8A8UNorm:
                case VkFormat.B8G8R8A8UScaled:
                case VkFormat.B8G8R8G8422UNorm:
                case VkFormat.B8G8R8SInt:
                case VkFormat.B8G8R8SNorm:
                case VkFormat.B8G8R8SScaled:
                case VkFormat.B8G8R8Srgb:
                case VkFormat.B8G8R8UInt:
                case VkFormat.B8G8R8UNorm:
                case VkFormat.B8G8R8UScaled:
                case VkFormat.BC1RGBASrgbBlock:
                case VkFormat.BC1RGBSrgbBlock:
                case VkFormat.BC1RgbUNormBlock:
                case VkFormat.BC1RgbaUNormBlock:
                case VkFormat.BC4SNormBlock:
                case VkFormat.BC4UNormBlock:
                case VkFormat.BC5SNormBlock:
                case VkFormat.BC5UNormBlock:
                case VkFormat.BC6HSFloatBlock:
                case VkFormat.BC6HUFloatBlock:
                case VkFormat.BC7SrgbBlock:
                case VkFormat.BC7UNormBlock:
                case VkFormat.D16UNorm:
                case VkFormat.D16UNormS8UInt:
                case VkFormat.D24UNormS8UInt:
                case VkFormat.D32SFloat:
                case VkFormat.D32SFloatS8UInt:
                case VkFormat.E5B9G9R9UFloatPack32:
                case VkFormat.EACR11G11SNormBlock:
                case VkFormat.EACR11G11UNormBlock:
                case VkFormat.EACR11SNormBlock:
                case VkFormat.EACR11UNormBlock:
                case VkFormat.ETC2R8G8B8A1SrgbBlock:
                case VkFormat.ETC2R8G8B8A1UNormBlock:
                case VkFormat.ETC2R8G8B8A8SrgbBlock:
                case VkFormat.ETC2R8G8B8A8UNormBlock:
                case VkFormat.ETC2R8G8B8SrgbBlock:
                case VkFormat.G10X6B10X6G10X6R10X6422UNorm4Pack16:
                case VkFormat.G10X6B10X6R10X62Plane420UNorm3Pack16:
                case VkFormat.G10X6B10X6R10X62Plane422UNorm3Pack16:
                case VkFormat.G10X6B10X6R10X63Plane420UNorm3Pack16:
                case VkFormat.G10X6B10X6R10X63Plane422UNorm3Pack16:
                case VkFormat.G10X6B10X6R10X63Plane444UNorm3Pack16:
                case VkFormat.G12X4B12X4G12X4R12X4422UNorm4Pack16:
                case VkFormat.G12X4B12X4R12X42Plane420UNorm3Pack16:
                case VkFormat.G12X4B12X4R12X42Plane422UNorm3Pack16:
                case VkFormat.G12X4B12X4R12X43Plane420UNorm3Pack16:
                case VkFormat.G12X4B12X4R12X43Plane422UNorm3Pack16:
                case VkFormat.G12X4B12X4R12X43Plane444UNorm3Pack16:
                case VkFormat.G16B16G16R16422UNorm:
                case VkFormat.G16B16R162Plane420UNorm:
                case VkFormat.G16B16R162Plane422UNorm:
                case VkFormat.G16B16R163Plane420UNorm:
                case VkFormat.G16B16R163Plane422UNorm:
                case VkFormat.G16B16R163Plane444UNorm:
                case VkFormat.G8B8G8R8422UNorm:
                case VkFormat.G8B8R82Plane420UNorm:
                case VkFormat.G8B8R82Plane422UNorm:
                case VkFormat.G8B8R83Plane420UNorm:
                case VkFormat.G8B8R83Plane422UNorm:
                case VkFormat.G8B8R83Plane444UNorm:
                case VkFormat.PVRTC12BppSrgbBlockImg:
                case VkFormat.PVRTC12BppUNormBlockImg:
                case VkFormat.PVRTC14BppSrgbBlockImg:
                case VkFormat.PVRTC14BppUNormBlockImg:
                case VkFormat.PVRTC22BppSrgbBlockImg:
                case VkFormat.PVRTC22BppUNormBlockImg:
                case VkFormat.PVRTC24BppSrgbBlockImg:
                case VkFormat.PVRTC24BppUNormBlockImg:
                case VkFormat.R10X6G10X6B10X6A10X6UNorm4Pack16:
                case VkFormat.R10X6G10X6UNorm2Pack16:
                case VkFormat.R10X6UNormPack16:
                case VkFormat.R12X4G12X4B12X4A12X4UNorm4Pack16:
                case VkFormat.R12X4G12X4UNorm2Pack16:
                case VkFormat.R12X4UNormPack16:
                case VkFormat.R16G16B16A16SFloat:
                case VkFormat.R16G16B16A16SInt:
                case VkFormat.R16G16B16A16SNorm:
                case VkFormat.R16G16B16A16SScaled:
                case VkFormat.R16G16B16A16UInt:
                case VkFormat.R16G16B16A16UNorm:
                case VkFormat.R16G16B16A16UScaled:
                case VkFormat.R16G16B16SFloat:
                case VkFormat.R16G16B16SInt:
                case VkFormat.R16G16B16SNorm:
                case VkFormat.R16G16B16SScaled:
                case VkFormat.R16G16B16UInt:
                case VkFormat.R16G16B16UNorm:
                case VkFormat.R16G16B16UScaled:
                case VkFormat.R16G16SFloat:
                case VkFormat.R16G16SInt:
                case VkFormat.R16G16SNorm:
                case VkFormat.R16G16SScaled:
                case VkFormat.R16G16UInt:
                case VkFormat.R16G16UNorm:
                case VkFormat.R16G16UScaled:
                case VkFormat.R16SNorm:
                case VkFormat.R16SScaled:
                case VkFormat.R16UNorm:
                case VkFormat.R16UScaled:
                case VkFormat.R32G32B32A32SFloat:
                case VkFormat.R32G32B32A32SInt:
                case VkFormat.R32G32B32A32UInt:
                case VkFormat.R32G32B32SFloat:
                case VkFormat.R32G32B32SInt:
                case VkFormat.R32G32B32UInt:
                case VkFormat.R32G32SFloat:
                case VkFormat.R32G32SInt:
                case VkFormat.R32G32UInt:
                case VkFormat.R32SInt:
                case VkFormat.R32UInt:
                case VkFormat.R4G4B4A4UNormPack16:
                case VkFormat.R4G4UNormPack8:
                case VkFormat.R5G5B5A1UNormPack16:
                case VkFormat.R5G6B5UNormPack16:
                case VkFormat.R64G64B64A64SFloat:
                case VkFormat.R64G64B64A64SInt:
                case VkFormat.R64G64B64A64UInt:
                case VkFormat.R64G64B64SFloat:
                case VkFormat.R64G64B64SInt:
                case VkFormat.R64G64B64UInt:
                case VkFormat.R64G64SFloat:
                case VkFormat.R64G64SInt:
                case VkFormat.R64G64UInt:
                case VkFormat.R64SFloat:
                case VkFormat.R64SInt:
                case VkFormat.R64UInt:
                case VkFormat.R8G8B8SInt:
                case VkFormat.R8G8B8SNorm:
                case VkFormat.R8G8B8SScaled:
                case VkFormat.R8G8B8UInt:
                case VkFormat.R8G8B8UScaled:
                case VkFormat.R8G8SInt:
                case VkFormat.R8G8SNorm:
                case VkFormat.R8G8SScaled:
                case VkFormat.R8G8Srgb:
                case VkFormat.R8G8UInt:
                case VkFormat.R8G8UScaled:
                case VkFormat.R8SInt:
                case VkFormat.R8SNorm:
                case VkFormat.R8SScaled:
                case VkFormat.R8Srgb:
                case VkFormat.R8UScaled:
                case VkFormat.S8UInt:
                case VkFormat.X8D24UNormPack32:
                case VkFormat.Undefined:
                case VkFormat.MaxEnum:
                default:
#if DEBUG
                    Debug.LogError(@"You're trying to load an untested/unsupported format. Please enter the correct format conversion in `KtxNativeInstance.cs`, test it and make a pull request. Otherwise please open an issue with a sample file.");
#endif
                    return GraphicsFormat.None;
            }
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

        [DllImport(INTERFACE_DLL)]
        static extern VkFormat ktx_get_vkFormat ( System.IntPtr ktxTexture );

        /*

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
