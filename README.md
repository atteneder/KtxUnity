# KtxUnity

[![openupm](https://img.shields.io/npm/v/com.atteneder.ktx?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.atteneder.ktx/)

Unity package that allows users to load [KTX 2.0](https://github.com/KhronosGroup/KTX-Software) or [Basis Universal](https://github.com/BinomialLLC/basis_universal) texture files.

## Features

- [Basis Universal](https://github.com/BinomialLLC/basis_universal) files (.basis)
- [KTX 2.0](https://github.com/KhronosGroup/KTX-Software) files (.ktx)
- ETC1s and UASTC mode for Basis Universal super compression
- Arbitrary Texture orientation can be considered

Following build targets are supported

- WebGL
- iOS (arm64 and armv7a)
- Android (arm64 and armv7a)
- Windows (64 bit)
- Universal Windows Platform (x64)
- macOS (Intel 64 bit)
- Linux (64 bit)

![Screenshot of loaded fish textures](https://github.com/atteneder/BasisUniversalUnityDemo/raw/master/Images/fishes.png "Lots of fish basis universal textures loaded via BasisUniversalUnity")

Thanks to [Khronos](https://www.khronos.org), [Binomial](http://www.binomial.info) and everyone involved in making KTX and Basis Universal available!

## Installing

The easiest way to install is to download and open the [Installer Package](https://package-installer.glitch.me/v1/installer/Atteneder/com.atteneder.ktx?registry=https%3A%2F%2Fpackage.openupm.com&scope=com.atteneder)

It runs a script that installs KtxUnity via a [scoped registry](https://docs.unity3d.com/Manual/upm-scoped.html). After that it is listed in the *Package Manager* and can be updated from there.

<details><summary>Alternative / Legacy installations (for Unity 2018.4 and older)</summary>

Install manually via package URL

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
    "com.unity.modules.unitywebrequest": "1.0.0"
    ...
  }
}
```

Next time you open your project in Unity, it will download the package automatically. You have to have a GIT LFS client (large file support) installed on your system. Otherwise you will get an error that the native library file (dll on Windows) is corrupt. There's more detail about how to add packages via GIT URLs in the [Unity documentation](https://docs.unity3d.com/Manual/upm-git.html).

</details>

## Using

There's a demo project that shows how you can use it:

<https://github.com/atteneder/KtxUnityDemo>

### Load from file

Excerpt from [KtxUnityDemo](https://github.com/atteneder/KtxUnityDemo/blob/master/Assets/Scripts/CustomKtxFileLoader.cs) how to load a file (for example from StreamingAssets):

```csharp
using UnityEngine;
using KtxUnity;

public class CustomKtxFileLoader : TextureFileLoader<KtxTexture>
{
    protected override void ApplyTexture(Texture2D texture, TextureOrientation orientation) {
        var renderer = GetComponent<Renderer>();
        if(renderer!=null && renderer.sharedMaterial!=null) {
            renderer.material.mainTexture = texture;
            // Optional: Support arbitrary texture orientation by flipping the texture if necessary
            var scale = renderer.material.mainTextureScale;
            scale.x = orientation.IsXFlipped() ? -1 : 1;
            scale.y = orientation.IsYFlipped() ? -1 : 1;
            renderer.material.mainTextureScale = scale;
        }
    }
}
```

In this case the base MonoBehaviour `TextureFileLoader` has a public `filePath` member, starts loading in `Start` and  already takes care of all things. You only need to implement the `ApplyTexture` method and do something with your texture.

`TextureOrientation` is used to counter-act a potentially flipped image by setting texture scales to negative one.

`TextureFileLoader` is generic and can load KTX or Basis Universal files. Depending on what you need, pass `KtxTexture` or `BasisUniversalTexture` into its type parameter.

### Using as Sprite

If you want to use the texture in a UI / Sprite context, this is how you create a Sprite with correct orientation (excerpt from [BasisImageLoader](https://github.com/atteneder/KtxUnityDemo/blob/main/Assets/Scripts/BasisImageLoader.cs)):

```csharp
    …
    protected override void ApplyTexture(Texture2D texture, TextureOrientation orientation)
    {
        Vector2 pos = new Vector2(0,0);
        Vector2 size = new Vector2(texture.width, texture.height);

        if(orientation.IsXFlipped()) {
            pos.x = size.x;
            size.x *= -1;
        }

        if(orientation.IsYFlipped()) {
            pos.y = size.y;
            size.y *= -1;
        }
        var sprite = Sprite.Create(texture, new Rect(pos, size), Vector2.zero);
        GetComponent<Image>().sprite = sprite;
    }
    …
```

### Load from URL

Loading from URLs is similar. Use `TextureUrlLoader`, which has the exact same interface as `TextureFileLoader`. In this example we load a Basis Universal texture via URL:

```C#
using UnityEngine;
using KtxUnity;

public class CustomBasisUniversalUrlLoader : TextureUrlLoader<BasisUniversalTexture>
{
   protected override void ApplyTexture(Texture2D texture, TextureOrientation orientation) {
        var renderer = GetComponent<Renderer>();
        if(renderer!=null && renderer.sharedMaterial!=null) {
            renderer.material.mainTexture = texture;
        }
    }
}
```

### Advanced

Developers who want to create advanced loading code should look into classes `KtxTexture`/`BasisUniversalTexture` and `TextureBase` directly.

## Creating Textures

You can use the command line tools `toktx` (comes with [KTX-Software](https://github.com/KhronosGroup/KTX-Software)) to create KTX v2.0 files and `basisu` (part of [Basis Universal](https://github.com/BinomialLLC/basis_universal)) to create .basis files.

The default texture orientation of both of those tools (right-down) does not match Unity's orientation (right-up). To counter-act, you can provide a parameter to flip textures in the vertical axis (Y). This is recommended, if you use the textures in Unity only. The parameters are:

- `--lower_left_maps_to_s0t0` for `toktx`
- `--y_flip` for `basisu`

Example usage:

```bash
# For KTX files:
# Create regular KTX file from an input image
toktx --bcmp regular.ktx input.png
# Create a y-flipped KTX file, fit for Unity out of the box
toktx --lower_left_maps_to_s0t0 --bcmp unity_flipped.ktx input.png


# For Basis files:
# Create regular basis file from an input image
basisu -output_file regular.basis input.png
# Create a y-flipped basis file, fit for Unity out of the box
basisu -y_flip -output_file unity_flipped.basis input.png
```

If changing the orientation of your texture files is not an option, you can correct it by applying it flipped at run-time (see [Usage](#using)).

## Limitations

At the moment known shortcomings:

- KTX with non-supercompressed formats (like uncompressed, DXT, BC7, PVRTC, ETC, … ) are not tested or supported
- Only 2D image texture types (no cube-map, videos, 3D texture, 2D arrays)
- Only RGB/RGBA is tested (RG,R untested)

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
