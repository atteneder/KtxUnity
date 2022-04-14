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

    public class BasisUniversalTexture : TextureBase
    {
        public override async Task<TextureResult> LoadBytesRoutine(NativeSlice<byte> data, bool linear = false) {

            var transcoder = BasisUniversal.GetTranscoderInstance();

            while(transcoder==null) {
                await Task.Yield();
                transcoder = BasisUniversal.GetTranscoderInstance();
            }

            TextureResult result;

            if(transcoder.Open(data)) {
                var textureType = transcoder.GetTextureType();
                if(textureType == BasisUniversalTextureType.Image2D) {
                    result = await TranscodeImage2D(transcoder,data,linear);
                    result.orientation = TextureOrientation.KTX_DEFAULT;
                    if(!transcoder.GetYFlip()) {
                        // Regular basis files (no y_flip) seem to be 
                        result.orientation |= TextureOrientation.Y_UP;
                    }
                } else {
#if DEBUG
                    Debug.LogErrorFormat("Basis Universal texture type {0} is not supported",textureType);
#endif
                    result = new TextureResult(ErrorCode.UnsupportedFormat);
                }
            } else {
                result = new TextureResult(ErrorCode.LoadingFailed);
            }

            BasisUniversal.ReturnTranscoderInstance(transcoder);

            return result;
        }

        async Task<TextureResult> TranscodeImage2D(BasisUniversalTranscoderInstance transcoder, NativeSlice<byte> data, bool linear) {
            
            TextureResult result = null;
            
            // Can turn to parameter in future
            uint imageIndex = 0;

            var meta = transcoder.LoadMetaData();

            var formats = GetFormat( meta, meta.images[imageIndex].levels[0], linear );

            if(formats.HasValue) {
#if KTX_VERBOSE
                Debug.LogFormat("Transcode to GraphicsFormat {0} ({1})",formats.Value.format,formats.Value.transcodeFormat);
#endif
                Profiler.BeginSample("BasisUniversalJob");
                var job = new BasisUniversalJob();

                job.imageIndex = imageIndex;

                job.result = new NativeArray<bool>(1,KtxNativeInstance.defaultAllocator);

                var jobHandle = BasisUniversal.LoadBytesJob(
                    ref job,
                    transcoder,
                    data,
                    formats.Value.transcodeFormat
                    );

                Profiler.EndSample();
                
                while(!jobHandle.IsCompleted) {
                    await Task.Yield();
                }
                jobHandle.Complete();

                if(job.result[0]) {
                    Profiler.BeginSample("LoadBytesRoutineGPUupload");
                    uint width;
                    uint height;
                    meta.GetSize(out width,out height);
                    var flags = TextureCreationFlags.None;
                    if(meta.images[imageIndex].levels.Length>1) {
                        flags |= TextureCreationFlags.MipChain;
                    }

                    result = new TextureResult {
                        texture = new Texture2D((int)width,(int)height,formats.Value.format,flags)
                    };
                    result.texture.LoadRawTextureData(job.textureData);
                    result.texture.Apply(false,true);
                    Profiler.EndSample();
                } else {
                    result = new TextureResult(ErrorCode.TranscodeFailed);
                }
                job.sizes.Dispose();
                job.offsets.Dispose();
                job.textureData.Dispose();
                job.result.Dispose();
            } else {
                result = new TextureResult(ErrorCode.UnsupportedFormat);
            }

            return result;
        }
    }
}
