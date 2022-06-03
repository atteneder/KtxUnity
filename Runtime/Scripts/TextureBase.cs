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

#if !(UNITY_ANDROID || UNITY_WEBGL) || UNITY_EDITOR
#define LOCAL_LOADING
#endif

using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;
using Unity.Collections;

namespace KtxUnity {
    public abstract class TextureBase
    {
        protected GraphicsFormat m_Format;
        
        /// <summary>
        /// Loads a KTX or Basis Universal texture from the StreamingAssets folder
        /// see https://docs.unity3d.com/Manual/StreamingAssets.html
        /// </summary>
        /// <param name="filePath">Path to the file, relative to StreamingAssets</param>
        public async Task<TextureResult> LoadFromStreamingAssets( string filePath, bool linear = false ) {
            var url = GetStreamingAssetsUrl(filePath);
            return await LoadFile(url,linear);
        }

        /// <summary>
        /// Loads a KTX or Basis Universal texture from an URL
        /// </summary>
        /// <param name="url">URL to the ktx/basis file to load</param>
        public async Task<TextureResult> LoadFromUrl( string url, bool linear = false ) {
            return await LoadFile(url,linear);
        }

        /// <summary>
        /// Load a KTX or Basis Universal texture from a buffer
        /// </summary>
        /// <param name="data">Native buffer that holds the ktx/basisu file</param>
        public async Task<TextureResult> LoadFromBytes( NativeSlice<byte> data, bool linear = false ) {
            return await LoadBytesRoutine(data,linear);
        }

        /// <summary>
        /// Converts a relative sub path within StreamingAssets
        /// and creates an absolute URI from it. Useful for loading
        /// via UnityWebRequests.
        /// </summary>
        /// <param name="subPath">Path, relative to StreamingAssets. Example: path/to/file.ktx</param>
        /// <returns>Platform independent URI that can be loaded via UnityWebRequest</returns>
        public static string GetStreamingAssetsUrl( string subPath ) {

            var path = Path.Combine(Application.streamingAssetsPath,subPath);

            #if LOCAL_LOADING
            path = string.Format( "file://{0}", path );
            #endif

            return path;
        }
        
        /// <summary>
        /// Loads, transcodes and creates a <see cref="Texture2D"/> from a
        /// texture in memory.
        /// </summary>
        /// <param name="data">Input texture data</param>
        /// <param name="linear">Depicts if texture is sampled in linear or
        /// sRGB gamma color space.</param>
        /// <returns><see cref="TextureResult"/> containg the result and
        /// (if loading failed) an <see cref="ErrorCode"/></returns>
        public async Task<TextureResult> LoadBytesRoutine(NativeSlice<byte> data, bool linear = false) {
            var result = new TextureResult();
            result.errorCode = Load(data);
            if (result.errorCode != ErrorCode.Success) return result;
            result.errorCode = await Transcode(linear);
            if (result.errorCode != ErrorCode.Success) return result;
            result = CreateTexture();
            Dispose();
            return result;
        }
        
        /// <summary>
        /// Loads a texture from memory. 
        /// Part of the low-level API that provides finer control over the
        /// loading process.
        /// <seealso cref="Transcode"/>
        /// <seealso cref="CreateTexture"/>
        /// <seealso cref="Dispose"/>
        /// </summary>
        /// <param name="data">Input texture data</param>
        /// <returns><see cref="ErrorCode.Success"/> if loading was successful
        /// or an error specific code otherwise.</returns>
        public abstract ErrorCode Load(NativeSlice<byte> data);
        
        /// <summary>
        /// Transcodes or decodes the texture into a GPU compatible format.
        /// Part of the low-level API that provides finer control over the
        /// loading process.
        /// <seealso cref="Load"/>
        /// <seealso cref="CreateTexture"/>
        /// <seealso cref="Dispose"/>
        /// </summary>
        /// <param name="linear">Depicts if texture is sampled in linear or
        /// sRGB gamma color space.</param>
        /// <returns></returns>
        public abstract Task<ErrorCode> Transcode(bool linear = false);
        
        /// <summary>
        /// Tries to create a <see cref="Texture2D"/> from the previously
        /// decoded texture.
        /// Part of the low-level API that provides finer control over the
        /// loading process.
        /// <seealso cref="Load"/>
        /// <seealso cref="Transcode"/>
        /// <seealso cref="Dispose"/>
        /// </summary>
        /// <returns></returns>
        public abstract TextureResult CreateTexture();
        
        /// <summary>
        /// Releases all resources.
        /// Part of the low-level API that provides finer control over the
        /// loading process.
        /// <seealso cref="Load"/>
        /// <seealso cref="Transcode"/>
        /// <seealso cref="Dispose"/>
        /// </summary>
        public abstract void Dispose();

        async Task<TextureResult> LoadFile( string url, bool linear = false ) {
    
            var webRequest = UnityWebRequest.Get(url);
            var asyncOp = webRequest.SendWebRequest();
            while (!asyncOp.isDone) {
                await Task.Yield();
            }

            if(!string.IsNullOrEmpty(webRequest.error)) {
#if DEBUG
                Debug.LogErrorFormat("Error loading {0}: {1}",url,webRequest.error);
#endif
                return new TextureResult(ErrorCode.OpenUriFailed);
            }

            var buffer = webRequest.downloadHandler.data;

            var na = new NativeArray<byte>(buffer,KtxNativeInstance.defaultAllocator);
            var result = await LoadBytesRoutine(na,linear);
            na.Dispose();
            return result;
        }

        protected virtual TranscodeFormatTuple? GetFormat( IMetaData meta, ILevelInfo li, bool linear = false ) {
            return TranscodeFormatHelper.GetFormatsForImage(meta,li,linear);
        }
    }
}
