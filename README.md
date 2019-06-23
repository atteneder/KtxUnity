# BasisUniversalUnity

Unity package that allows users to load [Basis Universal](https://github.com/BinomialLLC/basis_universal) texture files.

Special thanks to [Binomial](http://www.binomial.info) and everyone involved in making Basis Universal available!

## Installing

You have to manually add the package's URL into your [project manifest](https://docs.unity3d.com/Manual/upm-manifestPrj.html)

Inside your Unity project there's the folder `Packages` containing a file called `manifest.json`. You have to open it and add the following line inside the `dependencies` category:

```json
"com.atteneder.basisu": "https://github.com/atteneder/BasisUniversalUnity.git",
```

It should look something like this:

```json
{
  "dependencies": {
    "com.atteneder.basisu": "https://github.com/atteneder/BasisUniversalUnity.git",
    "com.unity.package-manager-ui": "2.1.2",
    "com.unity.modules.unitywebrequest": "1.0.0"
    ...
  }
}
```

There's more detail about how to add packages via GIT URLs in the [Unity documentation](https://docs.unity3d.com/Manual/upm-git.html).

## Using

There's a simple demo project that shows how you can use it:

<https://github.com/atteneder/BasisUniversalUnityDemo>

Excerpt how to load a file from StreamingAssets:

```C#
using UnityEngine;
using BasisUniversalUnity;

public class CustomBasisFileLoader : BasisFileLoader
{
    protected override void ApplyTexture(Texture2D texture) {
        var renderer = GetComponent<Renderer>();
        if(renderer!=null && renderer.sharedMaterial!=null) {
            renderer.material.mainTexture = texture;
        }
    }
}
```

In this simple case the base MonoBehaviour `BasisFileLoader` has a public `filePath` member, starts loading in `Start` and  already takes care of all things. You only need to implement the `ApplyTexture` method and do something with your texture.

Loading from URLs is similarly easy:

```C#
using UnityEngine;
using BasisUniversalUnity;

public class CustomBasisUrlLoader : BasisUrlLoader
{
   protected override void ApplyTexture(Texture2D texture) {
        var renderer = GetComponent<Renderer>();
        if(renderer!=null && renderer.sharedMaterial!=null) {
            renderer.material.mainTexture = texture;
        }
    }
}
```

Devs who want to create advanced loading code should look into class `BasisUniversal` directly.

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
