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

using System.Collections;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace KtxUnity {

    public class BasisUniversalTexture : TextureBase
    {
        public override IEnumerator LoadBytesRoutine(NativeSlice<byte> data) {

            uint imageIndex = 0;

            Texture2D texture = null;
            
            var transcoder = BasisUniversal.GetTranscoderInstance();

            while(transcoder==null) {
                yield return null;
                transcoder = BasisUniversal.GetTranscoderInstance();
            }

            if(transcoder.Open(data)) {
                var meta = transcoder.LoadMetaData();

                var formats = GetFormat( meta, meta.images[imageIndex].levels[0] );

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
                        yield return null;
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
                        texture = new Texture2D((int)width,(int)height,formats.Value.format,flags);
                        texture.LoadRawTextureData(job.textureData);
                        texture.Apply(false,true);
                        Profiler.EndSample();
                    } else {
                        Debug.LogError(ERR_MSG_TRANSCODE_FAILED);
                    }
                    job.offsets.Dispose();
                    job.textureData.Dispose();
                    job.result.Dispose();
                }
            }
            
            BasisUniversal.ReturnTranscoderInstance(transcoder);

            OnTextureLoaded(texture);
        }
    }
}
