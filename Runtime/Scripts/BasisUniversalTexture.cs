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
        
        public override ErrorCode Load(NativeSlice<byte> data) {
            m_InputData = data;
            return ErrorCode.Success;
        }

        public override async Task<ErrorCode> Transcode(bool linear = false) {
            var transcoder = BasisUniversal.GetTranscoderInstance();

            while(transcoder==null) {
                await Task.Yield();
                transcoder = BasisUniversal.GetTranscoderInstance();
            }

            ErrorCode result;

            if(transcoder.Open(m_InputData)) {
                var textureType = transcoder.GetTextureType();
                if(textureType == BasisUniversalTextureType.Image2D) {
                    result = await Transcode(transcoder,m_InputData,linear);
                    m_Orientation = TextureOrientation.KTX_DEFAULT;
                    if(!transcoder.GetYFlip()) {
                        // Regular basis files (no y_flip) seem to be 
                        m_Orientation |= TextureOrientation.Y_UP;
                    }
                } else {
#if DEBUG
                    Debug.LogErrorFormat("Basis Universal texture type {0} is not supported",textureType);
#endif
                    result = ErrorCode.UnsupportedFormat;
                }
            } else {
                result = ErrorCode.LoadingFailed;
            }

            BasisUniversal.ReturnTranscoderInstance(transcoder);

            return result;
        }

        public override TextureResult CreateTexture() {
            Profiler.BeginSample("LoadBytesRoutineGpuUpload");
            
            // Can turn to parameter in future
            const uint imageIndex = 0;

            m_MetaData.GetSize(out var width,out var height);
            var flags = TextureCreationFlags.None;
            if(m_MetaData.images[imageIndex].levels.Length>1) {
                flags |= TextureCreationFlags.MipChain;
            }

            var result = new TextureResult {
                texture = new Texture2D((int)width,(int)height,m_Format,flags),
                orientation = m_Orientation
            };
            result.texture.LoadRawTextureData(m_TextureData);
            result.texture.Apply(false,true);
            Profiler.EndSample();
            return result;
        }

        public override void Dispose() {
            m_TextureData.Dispose();
        }

        async Task<ErrorCode> Transcode(BasisUniversalTranscoderInstance transcoder, NativeSlice<byte> data, bool linear) {
            
            var result = ErrorCode.Success;
            
            // Can turn to parameter in future
            const uint imageIndex = 0;

            m_MetaData = transcoder.LoadMetaData();

            var formats = GetFormat( m_MetaData, m_MetaData.images[imageIndex].levels[0], linear );

            if(formats.HasValue) {
#if KTX_VERBOSE
                Debug.LogFormat("Transcode to GraphicsFormat {0} ({1})",formats.Value.format,formats.Value.transcodeFormat);
#endif
                Profiler.BeginSample("BasisUniversalJob");
                m_Format = formats.Value.format;
                var job = new BasisUniversalJob();

                job.imageIndex = imageIndex;

                job.result = new NativeArray<bool>(1,KtxNativeInstance.defaultAllocator);

                var jobHandle = BasisUniversal.LoadBytesJob(
                    ref job,
                    transcoder,
                    formats.Value.transcodeFormat
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
