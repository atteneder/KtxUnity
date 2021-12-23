// Copyright (c) 2019-2021 Andreas Atteneder, All Rights Reserved.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Threading.Tasks;
    
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;
using System.Collections.Generic;

namespace KtxUnity {

    public class BasisUniversalTexture : TextureBase
    {
        public override async Task<TextureResult> LoadBytesRoutine(NativeSlice<byte> data, uint imageIndex = 0, uint mipLevel = 0, bool linear = false) {

            bool yFlipped = true;

            var transcoder = BasisUniversal.GetTranscoderInstance();

            while(transcoder==null) {
                await Task.Yield();
                transcoder = BasisUniversal.GetTranscoderInstance();
            }

            Texture2D texture = null;
            Texture2D textureAlpha = null;
            if (transcoder.Open(data)) {
                var textureType = transcoder.GetTextureType();
                if(textureType == BasisUniversalTextureType.Image2D) {
                    yFlipped = transcoder.GetYFlip();
                    List<Texture2D> textures = await TranscodeImage2D(transcoder, data, imageIndex, mipLevel, linear);
                    texture = textures[0];
                    if (textures.Count > 1)
                    {
                        textureAlpha = textures[1];
                    }
                } else {
                    Debug.LogErrorFormat("Basis Universal texture type {0} is not supported",textureType);
                }
            }

            BasisUniversal.ReturnTranscoderInstance(transcoder);

            var orientation = TextureOrientation.KTX_DEFAULT;
            if(!yFlipped) {
                // Regular basis files (no y_flip) seem to be 
                orientation |= TextureOrientation.Y_UP;
            }

            return new TextureResult(texture, orientation,textureAlpha);
        }

        async Task<List<Texture2D>> TranscodeImage2D(BasisUniversalTranscoderInstance transcoder, NativeSlice<byte> data, uint imageIndex=0, uint mipLevel=0,bool linear=false) {

            List<Texture2D> textures = new List<Texture2D>();
            Texture2D texture_Opaque = null;
            Texture2D texture_Alpha = null;

            // Can turn to parameter in future

            var meta = transcoder.LoadMetaData();
            if (imageIndex >= meta.images.Length)
            {
                imageIndex = 0;
            }
            if (mipLevel >= meta.images[imageIndex].levels.Length)
            {
                mipLevel = 0;
            }
            var formats = GetFormat( meta, meta.images[imageIndex].levels[0], linear );

            if(formats.HasValue) {
#if KTX_VERBOSE
                Debug.LogFormat("Transcode to GraphicsFormat {0} ({1})",formats.Value.format,formats.Value.transcodeFormat);
#endif
                Profiler.BeginSample("BasisUniversalJob");
                var job = new BasisUniversalJob();

                job.imageIndex = imageIndex;
                job.mipLevel = mipLevel;

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
                    if(job.mipChain) {
                        flags |= TextureCreationFlags.MipChain;
                    }
                    texture_Opaque = new Texture2D((int)width,(int)height,formats.Value.format,flags);
                    texture_Opaque.LoadRawTextureData(job.textureData);
                    texture_Opaque.Apply(false,true);
                    textures.Add(texture_Opaque);
                    if (job.textureDataAlpha.Length > 0)
                    {
                        texture_Alpha = new Texture2D((int)width, (int)height, formats.Value.format, flags);
                        texture_Alpha.LoadRawTextureData(job.textureDataAlpha);
                        texture_Alpha.Apply(false, true);
                        textures.Add(texture_Alpha);
                    }

                    Profiler.EndSample();
                } else {
                    Debug.LogError(ERR_MSG_TRANSCODE_FAILED);
                }
                job.sizes.Dispose();
                job.offsets.Dispose();
                job.textureData.Dispose();
                job.textureDataAlpha.Dispose();
                job.result.Dispose();
            }

            return textures;
        }
    }
}
