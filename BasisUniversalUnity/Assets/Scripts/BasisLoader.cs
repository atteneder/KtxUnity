#if !(UNITY_ANDROID || UNITY_WEBGL) || UNITY_EDITOR
#define LOCAL_LOADING
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class BasisLoader : MonoBehaviour
{
    public string filePath;
    public Texture2D texture;

    void Start() {
        StartCoroutine(LoadBasisFile());
    }

    IEnumerator LoadBasisFile() {
 
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

        Debug.LogFormat("loaded {0} bytes", bytes.Length);

        var basis = BasisUniversal.LoadBytes(bytes);

        uint width;
        uint height;

        basis.GetImageSize(out width, out height);
        Debug.LogFormat("image size {0} x {1}", width, height);

        bool hasAlpha = basis.GetHasAlpha();
        Debug.LogFormat("image has alpha {0}",hasAlpha);

        var imageCount = basis.GetImageCount();
        Debug.LogFormat("image count {0}",imageCount);

        for(uint i=0; i<imageCount;i++) {
            var levelCount = basis.GetLevelCount(i);
            Debug.LogFormat("level count image {0}: {1}",i,levelCount);
        }

        TextureFormat tf = TextureFormat.DXT1;
        BasisUniversal.TranscodeFormat transF = BasisUniversal.TranscodeFormat.BC1;

        if(hasAlpha) {
            if(SystemInfo.SupportsTextureFormat(TextureFormat.DXT5)) {
                tf=TextureFormat.DXT5;
                transF = BasisUniversal.TranscodeFormat.BC3;
            } else {
                Debug.LogError("No supported texture format found");
            }
        } else {
            if(SystemInfo.SupportsTextureFormat(TextureFormat.DXT1)) {
                tf=TextureFormat.DXT1;
                transF = BasisUniversal.TranscodeFormat.BC1;
            } else {
                Debug.LogError("No supported texture format found");
            }
        }
        byte[] trData;
        if(basis.Transcode(0,0,transF,out trData)) {
            Debug.LogFormat("transcoded {0} bytes", trData.Length);

            texture = new Texture2D((int)width,(int)height,tf,false);
            texture.LoadRawTextureData(trData);
            texture.Apply();

            var renderer = GetComponent<Renderer>();
            if(renderer!=null && renderer.sharedMaterial!=null) {
                renderer.material.mainTexture = texture;
            }
        }
    }
}
