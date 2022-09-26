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

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace KtxUnity {

    public class BasisUniversalTexture : TextureBase {

        NativeSlice<byte> m_InputData;
        NativeArray<byte> m_TextureData;
        MetaData m_MetaData;
        TextureOrientation m_Orientation;
        
        public override ErrorCode Open(NativeSlice<byte> data) {
            m_InputData = data;
            return ErrorCode.Success;
        }

        public override async Task<TextureResult> LoadTexture2D(
            bool linear = false,
            uint layer = 0,
            uint faceSlice = 0,
            uint mipLevel = 0,
            bool mipChain = true
            ) 
        {
            var transcoder = BasisUniversal.GetTranscoderInstance();

            while(transcoder==null) {
                await Task.Yield();
                transcoder = BasisUniversal.GetTranscoderInstance();
            }

            var result = new TextureResult();

            if(transcoder.Open(m_InputData)) {
                result.errorCode = await Transcode(
                    transcoder,
                    linear,
                    layer,
                    mipLevel,
                    mipChain
                    );
                m_Orientation = TextureOrientation.KTX_DEFAULT;
                if(!transcoder.GetYFlip()) {
                    // Regular basis files (no y_flip) seem to be 
                    m_Orientation |= TextureOrientation.Y_UP;
                }
                BasisUniversal.ReturnTranscoderInstance(transcoder);
            } else {
                BasisUniversal.ReturnTranscoderInstance(transcoder);
                result.errorCode = ErrorCode.LoadingFailed;
                return result;
            }

            Profiler.BeginSample("LoadBytesRoutineGpuUpload");
            
            m_MetaData.GetSize(out var width,out var height,layer,mipLevel);
            var flags = TextureCreationFlags.None;
            if(mipChain && m_MetaData.images[layer].levels.Length-mipLevel>1) {
                flags |= TextureCreationFlags.MipChain;
            }

            result.texture = new Texture2D((int)width, (int)height, m_Format, flags);
            result.orientation = m_Orientation;
            
#if KTX_UNITY_GPU_UPLOAD
            // TODO: native GPU upload
#else
#endif
            
            result.texture.LoadRawTextureData(m_TextureData);
            result.texture.Apply(false,true);
            Profiler.EndSample();
            return result;
        }

        public override void Dispose() {
            m_TextureData.Dispose();
        }

        async Task<ErrorCode> Transcode(
            BasisUniversalTranscoderInstance transcoder,
            bool linear,
            uint layer,
            uint mipLevel,
            bool mipChain
            )
        {
            
            var result = ErrorCode.Success;

            m_MetaData = transcoder.LoadMetaData();

            var formats = GetFormat( m_MetaData, m_MetaData.images[layer].levels[0], linear );

            if(formats.HasValue) {
#if KTX_VERBOSE
                Debug.LogFormat("LoadTexture2D to GraphicsFormat {0} ({1})",formats.Value.format,formats.Value.transcodeFormat);
#endif
                Profiler.BeginSample("BasisUniversalJob");
                m_Format = formats.Value.format;
                var job = new BasisUniversalJob {
                    layer = layer,
                    mipLevel = mipLevel,
                    result = new NativeArray<bool>(1,KtxNativeInstance.defaultAllocator)
                };

                var jobHandle = BasisUniversal.LoadBytesJob(
                    ref job,
                    transcoder,
                    formats.Value.transcodeFormat,
                    mipChain
                    );

                m_TextureData = job.textureData;
                Profiler.EndSample();
                
                while(!jobHandle.IsCompleted) {
                    await Task.Yield();
                }
                jobHandle.Complete();

                if(!job.result[0]) {
                    m_TextureData.Dispose();
                    result = ErrorCode.TranscodeFailed;
                }
                job.sizes.Dispose();
                job.offsets.Dispose();
                job.result.Dispose();
            } else {
                result = ErrorCode.UnsupportedFormat;
            }

            return result;
        }
    }
}
