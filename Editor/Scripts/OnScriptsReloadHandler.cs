// Copyright 2020-2021 Andreas Atteneder
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using UnityEngine;

namespace KtxUnity.Editor {

    public static class OnScriptsReloadHandler {

        const string k_PkgName = "KtxUnity";
        const string k_PkgLegacyVersion = "1.x";
        const string k_PkgMinVersion = "2.0.0";
        
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptsReloaded() {
#if UNITY_WEBGL && UNITY_2021_2_OR_NEWER
            Debug.LogError($"Update {k_PkgName} to version {k_PkgMinVersion} or newer!\nWith Unity 2021.2 or newer that is required for successful WebGL builds!");
#endif
        }
    }
}
