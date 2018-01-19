using System;
namespace Neb
{
    interface NebuleuseServer
    {
        ErrorResult ErrorCB{get;set;}
        void GetServiceStatus(StatusResult statusCB);
        void Connect(string name, string password, ConnectResult ConnectCB);
        void SubscribeTo(string pipe, string channel, ActionResult ActionCB);
        void GetLongPoll();
    }
}