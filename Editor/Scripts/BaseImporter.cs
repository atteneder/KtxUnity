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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace KtxUnity
{
    public abstract class TextureImporter : ScriptedImporter {
        
        /// <summary>
        /// Texture array layer to import.
        /// </summary>
        public uint layer;
        
        /// <summary>
        /// Cubemap face or 3D/volume texture slice to import.
        /// </summary>
        public uint faceSlice;
        
        /// <summary>
        /// Lowest mipmap level to import (where 0 is the highest resolution).
        /// Lower mipmap levels (of higher resolution) are being discarded.
        /// Useful to limit texture resolution.
        /// </summary>
        public uint levelLowerLimit;
        
        /// <summary>
        /// If true, a mipmap chain (if present) is imported.
        /// </summary>
        public bool importLevelChain = true;
        
        /// <summary>
        /// If true, texture will be sampled
        /// in linear color space (sRGB otherwise)
        /// </summary>
        public bool linear;
        
        // ReSharper disable once NotAccessedField.Local
        [SerializeField][HideInInspector]
        string[] reportItems;
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Profiler.BeginSample("Import Texture");
            var texture = CreateTextureBase();
            Profiler.BeginSample("Load Texture");
            var result = AsyncHelpers.RunSync(() =>
            {
                using (var alloc = new ManagedNativeArray(File.ReadAllBytes(assetPath))) {
                    return texture.LoadFromBytes(
                        alloc.nativeArray,
                        linear,
                        layer,
                        faceSlice,
                        levelLowerLimit,
                        importLevelChain
                        );
                }
            });
            Profiler.EndSample();

            if (result.errorCode == ErrorCode.Success) {
                result.texture.name = name;
                result.texture.alphaIsTransparency = true;
                ctx.AddObjectToAsset("result", result.texture);
                ctx.SetMainObject(result.texture);
                reportItems = new string[] { };
            } else {
                var errorMessage = ErrorMessage.GetErrorMessage(result.errorCode);
                reportItems = new[] { errorMessage };
                Debug.LogError($"Could not load texture file at {assetPath}: {errorMessage}",this);
            }
            
            Profiler.EndSample();
        }
        
        protected abstract TextureBase CreateTextureBase();
        
        // from glTFast : AsyncHelpers
        static class AsyncHelpers
        {
            /// <summary>
            /// Executes an async Task&lt;T&gt; method which has a T return type synchronously
            /// </summary>
            /// <typeparam name="T">Return Type</typeparam>
            /// <param name="task">Task&lt;T&gt; method to execute</param>
            /// <returns></returns>
            public static T RunSync<T>(Func<Task<T>> task)
            {
                var oldContext = SynchronizationContext.Current;
                var sync = new ExclusiveSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(sync);
                T ret = default(T);
                sync.Post(async _ =>
                {
                    try
                    {
                        ret = await task();
                    }
                    catch (Exception e)
                    {
                        sync.InnerException = e;
                        throw;
                    }
                    finally
                    {
                        sync.EndMessageLoop();
                    }
                }, null);
                sync.BeginMessageLoop();
                SynchronizationContext.SetSynchronizationContext(oldContext);
                return ret;
            }

            class ExclusiveSynchronizationContext : SynchronizationContext
            {
                bool done;
                public Exception InnerException { get; set; }
                readonly AutoResetEvent workItemsWaiting = new AutoResetEvent(false);
                readonly Queue<Tuple<SendOrPostCallback, object>> items =
                    new Queue<Tuple<SendOrPostCallback, object>>();

                public override void Send(SendOrPostCallback d, object state)
                {
                    throw new NotSupportedException("We cannot send to our same thread");
                }

                public override void Post(SendOrPostCallback d, object state)
                {
                    lock (items)
                    {
                        items.Enqueue(Tuple.Create(d, state));
                    }
                    workItemsWaiting.Set();
                }

                public void EndMessageLoop()
                {
                    Post(_ => done = true, null);
                }

                public void BeginMessageLoop()
                {
                    while (!done)
                    {
                        Tuple<SendOrPostCallback, object> task = null;
                        lock (items)
                        {
                            if (items.Count > 0)
                            {
                                task = items.Dequeue();
                            }
                        }
                        if (task != null)
                        {
                            task.Item1(task.Item2);
                            if (InnerException != null) // the method threw an exception
                            {
                                throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                            }
                        }
                        else
                        {
                            workItemsWaiting.WaitOne();
                        }
                    }
                }

                public override SynchronizationContext CreateCopy()
                {
                    return this;
                }
            }
        }
    }
}