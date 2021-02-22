using AltV.Net.Elements.Entities;
using AltV.Net.Elements.Pools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TSLipSync
{
    class AsyncFunctionCallback<T> : IAsyncBaseObjectCallback<T> where T : IBaseObject
    {
        private readonly Func<T, Task> _callback;

        public AsyncFunctionCallback(Func<T, Task> callback)
        {
            _callback = callback;
        }

        public Task OnBaseObject(T baseObject)
        {
            return _callback(baseObject);
        }
    }
}
