using System;
using System.Collections.Generic;

namespace Common
{

    public interface IServerRequest
    {
        long RequestId { get; set; }
    }

    public interface IServerRequestException : IServerRequest
    {
        string Message { get; }
    }

    public interface IServerRequestEngine
    {
        /// <summary>
        /// Make request to server
        /// </summary>
        /// <param name="request">request type</param>
        /// <param name="response">Response from server. If there is Exception on server, IServerRequestException comes</param>
        /// <returns>Response</returns>
        void MakeRequest(IServerRequest request, Action<IServerRequest> response);

        event EventHandler<EventArgs<IServerRequest>> SendRequestToServer;

        void ResponseFromServer(IServerRequest serverRequest);

        /// <summary>
        /// If connection's lost external system should infrom enigine via this method
        /// </summary>
        void Disconnect();

    }



    public class ServerRequestEngine : IServerRequestEngine 
    {

        internal class ServerRequestException : IServerRequestException
        {
            public ServerRequestException(long requestId, string message)
            {
                RequestId = requestId;
                Message = message;
            }

            #region Implementation of IServerRequest

            public long RequestId { get; set; }

            #endregion

            #region Implementation of IServerRequestException

            public string Message { get; private set; }

            #endregion
        }

        private long _requestId;

        private readonly Dictionary<long, Action<IServerRequest>> _activeRequests = new Dictionary<long, Action<IServerRequest>>();

        #region Implementation of IServerRequestEngine

        public void MakeRequest(IServerRequest request, Action<IServerRequest> response)
        {
            request.RequestId = _requestId++;

            _activeRequests.Add(request.RequestId, response);

            SendServerRequest(request);
        }

        protected void SendServerRequest(IServerRequest serverRequest)
        {
            var myEvent = SendRequestToServer;

            if (myEvent == null)
                throw new Exception("There is no subscribtion of Server Request Engine");

            myEvent(this, new EventArgs<IServerRequest>(serverRequest));
        }
        public event EventHandler<EventArgs<IServerRequest>> SendRequestToServer;

        public void ResponseFromServer(IServerRequest serverRequest)
        {
            var response = _activeRequests[serverRequest.RequestId];
            _activeRequests.Remove(serverRequest.RequestId);
            response(serverRequest);
        }

        public void Disconnect()
        {
            foreach (var activeRequest in _activeRequests)
                ResponseFromServer(new ServerRequestException(activeRequest.Key, "Connection Fail"));
        }

        #endregion
    }
}
