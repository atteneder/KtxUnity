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
    [ScriptedImporter(0, new []{ ".ktx2" })]
    public class KtxImporter : ScriptedImporter
    {
        public bool linear;
        
        // ReSharper disable once NotAccessedField.Local
        [SerializeField][HideInInspector]
        private string[] reportItems;
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Profiler.BeginSample("Import KTX2");
            var texture = new KtxTexture();
            Profiler.BeginSample("Load KTX2");
            var result = AsyncHelpers.RunSync(() =>
            {
                var alloc = new NativeArray<byte>(File.ReadAllBytes(assetPath), Allocator.Persistent);
                var data = texture.LoadFromBytes(alloc, linear);
                alloc.Dispose();
                return data;
            }).texture;
            Profiler.EndSample();

            if(!result)
            {
                // TODO needs a way to pipe issues back into the importer
                reportItems = new[] { "Couldn't import file. See the console output for more details." };
                result = new Texture2D(4, 4, TextureFormat.RGB24, true, linear);
                var arr = new Color32[result.width * result.height];
                for (int i = 0; i < arr.Length; i++) arr[i] = new Color32(255, 0, 0, 255);
                result.SetPixels32(arr);
                result.Apply();
            }
            result.name = this.name;
            ctx.AddObjectToAsset("result", result);
            ctx.SetMainObject(result);
            
            Profiler.EndSample();
        }
        
        // from glTFast : AsyncHelpers
        private static class AsyncHelpers
        {
            /// <summary>
            /// Executes an async Task<T> method which has a void return value synchronously
            /// </summary>
            /// <param name="task">Task<T> method to execute</param>
            public static void RunSync(Func<Task> task)
            {
                var oldContext = SynchronizationContext.Current;
                var synch = new ExclusiveSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(synch);
                synch.Post(async _ =>
                {
                    try
                    {
                        await task();
                    }
                    catch (Exception e)
                    {
                        synch.InnerException = e;
                        throw;
                    }
                    finally
                    {
                        synch.EndMessageLoop();
                    }
                }, null);
                synch.BeginMessageLoop();

                SynchronizationContext.SetSynchronizationContext(oldContext);
            }

            /// <summary>
            /// Executes an async Task<T> method which has a T return type synchronously
            /// </summary>
            /// <typeparam name="T">Return Type</typeparam>
            /// <param name="task">Task<T> method to execute</param>
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

            private class ExclusiveSynchronizationContext : SynchronizationContext
            {
                private bool done;
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