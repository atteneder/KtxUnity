# KtxUnity

Unity package that allows users to load [KTX 2.0](https://github.com/KhronosGroup/KTX-Software) files with [Basis Universal](https://github.com/BinomialLLC/basis_universal) super compression or [Basis Universal](https://github.com/BinomialLLC/basis_universal) texture files directly.

Following build targets are supported

- WebGL
- iOS
- Android (arm64 and armv7a)
- Windows (32 and 64 bit)
- macOS (64 bit)
- Linux (32 and 64 bit)

![Screenshot of loaded fish textures](https://github.com/atteneder/BasisUniversalUnityDemo/raw/master/Images/fishes.png "Lots of fish basis universal textures loaded via BasisUniversalUnity")

Thanks to [Khronos](https://www.khronos.org), [Binomial](http://www.binomial.info) and everyone involved in making KTX and Basis Universal available!

## Installing

You have to manually add the package's URL into your [project manifest](https://docs.unity3d.com/Manual/upm-manifestPrj.html)

Inside your Unity project there's the folder `Packages` containing a file called `manifest.json`. You have to open it and add the following line inside the `dependencies` category:

```json
"com.atteneder.ktx": "https://gitlab.com/atteneder/ktxunity.git",
```

It should look something like this:

```json
{
  "dependencies": {
    "com.atteneder.ktx": "https://gitlab.com/atteneder/ktxunity.git",
    "com.unity.package-manager-ui": "2.1.2",
    "com.unity.modules.unitywebrequest": "1.0.0"
    ...
  }
}
```

Next time you open your project in Unity, it will download the package automatically. You have to have a GIT LFS client (large file support) installed on your system. Otherwise you will get an error that the native library file (dll on Windows) is corrupt. There's more detail about how to add packages via GIT URLs in the [Unity documentation](https://docs.unity3d.com/Manual/upm-git.html).

## Using

There's a simple demo project that shows how you can use it:

<https://github.com/atteneder/KtxUnityDemo>

Excerpt how to load a file from StreamingAssets:

```C#
using UnityEngine;
using KtxUnity;

public class CustomKtxFileLoader : TextureFileLoader<KtxTexture>
{
    protected override void ApplyTexture(Texture2D texture) {
        var renderer = GetComponent<Renderer>();
        if(renderer!=null && renderer.sharedMaterial!=null) {
            renderer.material.mainTexture = texture;
        }
    }
}
```

In this simple case the base MonoBehaviour `TextureFileLoader` has a public `filePath` member, starts loading in `Start` and  already takes care of all things. You only need to implement the `ApplyTexture` method and do something with your texture.

`TextureFileLoader` is generic and can load KTX or Basis Universal files. Depending on what you need, pass `KtxTexture` or `BasisUniversalTexture` into its type parameter.

Loading from URLs is similarly easy, just use `TextureUrlLoader`, which has the exact same interface. In this example we load a Basis Universal texture via URL:

```C#
using UnityEngine;
using KtxUnity;

public class CustomBasisUniversalUrlLoader : TextureUrlLoader<BasisUniversalTexture>
{
   protected override void ApplyTexture(Texture2D texture) {
        var renderer = GetComponent<Renderer>();
        if(renderer!=null && renderer.sharedMaterial!=null) {
            renderer.material.mainTexture = texture;
        }
    }
}
```

Developers who want to create advanced loading code should look into classes `KtxTexture`/`BasisUniversalTexture` and `TextureBase` directly.

## Support

Like this demo? You can show your appreciation and ...

[![Buy me a coffee](https://az743702.vo.msecnd.net/cdn/kofi1.png?v=0)](https://ko-fi.com/C0C3BW7G)

## License

Copyright (c) 2019 Andreas Atteneder, All Rights Reserved.
Licensed under the Apache License, Version 2.0 (the "License");
you may not use files in this repository except in compliance with the License.
You may obtain a copy of the License at

   <http://www.apache.org/licenses/LICENSE-2.0>

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
