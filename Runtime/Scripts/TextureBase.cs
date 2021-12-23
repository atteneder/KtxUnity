﻿// Copyright (c) 2019-2021 Andreas Atteneder, All Rights Reserved.

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
        protected const string ERR_MSG_TRANSCODE_FAILED = "Transcoding failed!";

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
        public async Task<TextureResult> LoadFromBytes(NativeSlice<byte> data, uint imageIndex = 0, uint mipLevel = 0, bool linear = false) {
            return await LoadBytesRoutine(data,imageIndex,mipLevel,linear);
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

        async Task<TextureResult> LoadFile( string url, bool linear = false ) {
    
            var webRequest = UnityWebRequest.Get(url);
            var asyncOp = webRequest.SendWebRequest();
            while (!asyncOp.isDone) {
                await Task.Yield();
            }

            if(!string.IsNullOrEmpty(webRequest.error)) {
                Debug.LogErrorFormat("Error loading {0}: {1}",url,webRequest.error);
                return null;
            }

            var buffer = webRequest.downloadHandler.data;

            var na = new NativeArray<byte>(buffer,KtxNativeInstance.defaultAllocator);
            var result = await LoadBytesRoutine(na,0,0,linear);
            na.Dispose();
            return result;
        }

        public abstract Task<TextureResult> LoadBytesRoutine(NativeSlice<byte> data, uint imageIndex = 0, uint mipLevel = 0, bool linear = false);

        protected virtual TranscodeFormatTuple? GetFormat( IMetaData meta, ILevelInfo li, bool linear = false ) {
            return TranscodeFormatHelper.GetFormatsForImage(meta,li,linear);
        }
    }
}
