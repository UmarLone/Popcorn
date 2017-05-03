using System;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace Popcorn.Messaging.Async
{
    /// <summary>
    /// AsyncMessage Receiver
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    class AsyncMessageReceiver<TMessage> : IDisposable
        where TMessage : MessageBase
    {
        private IMessenger _messenger;
        private Func<TMessage, Task<object>> _callback;
        private object _token;
        public AsyncMessageReceiver(IMessenger messenger,
            object token,
            bool receiveDerivedMessagesToo,
            Func<TMessage, Task<object>> callback)
        {
            _messenger = messenger;
            _token = token;
            _callback = callback;
            messenger.Register<AsyncMessage<TMessage>>(
                this,
                token,
                receiveDerivedMessagesToo,
                ReceiveAsyncMessage);
        }

        private async void ReceiveAsyncMessage(AsyncMessage<TMessage> m)
        {
            try
            {
                var result = await _callback(m.InnerMessage);
                m.SetResult(result);
            }
            catch (Exception ex)
            {
                m.SetException(ex);
            }
        }
        public void Dispose()
        {
            if (_callback == null)
            {
                return;
            }
            _messenger.Unregister<AsyncMessage<TMessage>>(this, _token, ReceiveAsyncMessage);
            _callback = null;
            _token = null;
            _messenger = null;
        }
    }
}
