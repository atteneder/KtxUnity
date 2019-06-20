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

#if !(UNITY_ANDROID || UNITY_WEBGL) || UNITY_EDITOR
#define LOCAL_LOADING
#endif

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace BasisUniversalUnity {

    public class BasisUniversalTexture
    {
        public event UnityAction<Texture2D> onTextureLoaded;

        public BasisUniversalTexture() {
            BasisUniversal.Init();
        }

        public void Load( string filePath, MonoBehaviour monoBehaviour ) {
            monoBehaviour.StartCoroutine(LoadBasisFile(filePath));
        }

        IEnumerator LoadBasisFile(string filePath) {
    
            var path = Path.Combine(Application.streamingAssetsPath,filePath);

#if LOCAL_LOADING
            path = string.Format( "file://{0}", path );
#endif

            var webRequest = UnityWebRequest.Get(path);
            yield return webRequest.SendWebRequest();
            if(!string.IsNullOrEmpty(webRequest.error)) {
                yield break;
            }
            var bytes = webRequest.downloadHandler.data;

            Log("loaded {0} bytes", bytes.Length);

            var basis = BasisUniversal.LoadBytes(bytes);

            uint width;
            uint height;

            basis.GetImageSize(out width, out height);
            Log("image size {0} x {1}", width, height);

            bool hasAlpha = basis.GetHasAlpha();
            Log("image has alpha {0}",hasAlpha);

            var imageCount = basis.GetImageCount();
            Log("image count {0}",imageCount);

            for(uint i=0; i<imageCount;i++) {
                var levelCount = basis.GetLevelCount(i);
                Log("level count image {0}: {1}",i,levelCount);
            }

            TextureFormat tf;
            BasisUniversal.TranscodeFormat transF;

            if(!BasisUniversal.GetPreferredFormat(out tf,out transF,hasAlpha)) {
                Debug.LogError("No supported format found!\nRebuild with BASISU_VERBOSE scripting define to debug.");
                #if BASISU_VERBOSE
                BasisUniversal.CheckTextureSupport();
                #endif
                yield break;
            } else {
                Log("Trying to transcode to {0} ({1})",tf,transF);
            }

            byte[] trData;
            if(basis.Transcode(0,0,transF,out trData)) {
                Log("transcoded {0} bytes", trData.Length);

                var texture = new Texture2D((int)width,(int)height,tf,false);
                texture.LoadRawTextureData(trData);
                texture.Apply();

                if(onTextureLoaded!=null) {
                    onTextureLoaded(texture);
                }
            }
        }

        [System.Diagnostics.Conditional("BASISU_VERBOSE")]
        void Log(string format, params object[] args) {
            Debug.LogFormat(format,args);
        }
    }
}
