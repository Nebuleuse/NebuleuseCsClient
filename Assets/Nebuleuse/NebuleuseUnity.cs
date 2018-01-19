using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using UnityEngine;

using System;
using UnityEngine.Networking;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Collections.Specialized;
using System.Net;

namespace Neb
{
    public partial class Nebuleuse
    {
        class NebuleuseUnityServer : NebuleuseServer
        {
            public ErrorResult ErrorCB{get;set;}
            System.ComponentModel.AsyncOperation AsyncOp;
            protected static HttpClient _client;
            private string SessionID;

            public NebuleuseUnityServer(string host)
            {
                AsyncOp = AsyncOperationManager.CreateOperation(null);

                var handler = new HttpClientHandler { UseCookies = false };
                _client = new HttpClient(handler) { BaseAddress = new Uri(host) };
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
                _client.Timeout = TimeSpan.FromSeconds(20);
            }
#region General/Utility
            protected void setContentJson(HttpRequestMessage req, object o)
            {
                req.Content = new StringContent(JsonUtility.ToJson(o), Encoding.UTF8, "application/json");
            }

            protected HttpResponseMessage Get(string url)
            {
                var reqTask = _client.GetAsync(url);
                reqTask.Wait();
                return reqTask.Result;
            }

            protected HttpResponseMessage Post(string url, Dictionary<string, string> values)
            {

                var reqTask = _client.PostAsync(url, new FormUrlEncodedContent(values));
                reqTask.Wait();
                return reqTask.Result;
            }

            protected string ReadContent(HttpResponseMessage message)
            {
                var readTask = message.Content.ReadAsStringAsync();
                readTask.Wait();
                return readTask.Result;
            }

            protected bool ParseStatusCode(HttpResponseMessage response, string value)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return true;
                    default:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.InternalServerError:
                    try{
                        var result = JsonUtility.FromJson<ResponseModel>(value);
                        AsyncOp.Post((object o)=>{
                            ErrorCB(result.Code, result.Message);
                        },null);
                    }catch(Exception e){
                        AsyncOp.Post((object o)=>{
                            ErrorCB(NebuleuseError.NEBULEUSE_ERROR, value);
                        },null);
                    }
                        return false;
                }
            }
            #endregion

#region ServiceStatus

            public void GetServiceStatus(StatusResult StatusCB)
            {
                new Thread(() =>
                {
                    try{
                        var res = Get("/status");
                        
                        string json = ReadContent(res);
                        if(!ParseStatusCode(res, json))
                            return;

                        StatusModel model = JsonUtility.FromJson<StatusModel>(json);;

                        AsyncOp.Post((object o) =>
                        {
                            StatusCB(model);
                        }, null);
                    } catch(Exception e){
                        AsyncOp.Post((o) => {
                            ErrorCB(NebuleuseError.NEBULEUSE_ERROR_NETWORK, "Network error, server unreachable : " + e.ToString());
                        }, null);
                    }
                }).Start();
            }

            #endregion

#region Connect

            public void Connect(string username, string password, ConnectResult ConnectCB)
            {
                new Thread(() =>
                {
                    try{
                        var res = Post("/connect", new Dictionary<string, string>{
                            { "username", username },
                            { "password", password }
                       });

                        string json = ReadContent(res);
                        if(!ParseStatusCode(res, json))
                            return;

                        SessionID = JsonUtility.FromJson<ConnectModel>(json).SessionId;

                        AsyncOp.Post((o) => { ConnectCB(SessionID); }, null);
                    } catch(Exception e){
                        AsyncOp.Post((o) => {
                             ErrorCB(NebuleuseError.NEBULEUSE_ERROR_NETWORK, "Network error, server unreachable : " + e.ToString()); 
                        }, null);
                        
                    }
                }).Start();
            }
            #endregion
#region LongPoll
            public void GetLongPoll()
            {
                new Thread(() =>
                {
                    int emptyResponses = 0;
                    int count = 0;
                    while (emptyResponses < 3){
                        
                        Debug.Log("Polll");
                        HttpResponseMessage res;
                        try{
                            res = Post("/getMessages", new Dictionary<string, string>{
                                { "sessionid", SessionID },
                            });

                            string json = ReadContent(res);
                            if(!ParseStatusCode(res, json))
                                return;
                            
                            Debug.Log(count);
                            count++;
                            emptyResponses = 0;
                        }catch(Exception e){
                            emptyResponses++;
                            Thread.Sleep(10000);
                        }
                    }
                    AsyncOp.Post((o) => {
                        ErrorCB(NebuleuseError.NEBULEUSE_ERROR_NETWORK, "Network error, server unreachable");
                    }, null);
                }).Start();
            }
            #endregion

            public void SubscribeTo(string pipe, string channel, ActionResult ActionCB){
                new Thread(() =>
                {
                    try{
                        var res = Post("/subscribeTo", new Dictionary<string, string>{
                            { "sessionid", this.SessionID },
                            { "pipe", pipe },
                            { "channel", channel }
                       });

                        string json = ReadContent(res);
                        /*if(!ParseStatusCode(res, json))
                            return;*/

                        AsyncOp.Post((o) => { ActionCB(true, json); }, null);
                    } catch(Exception e){
                        AsyncOp.Post((o) => {
                             ErrorCB(NebuleuseError.NEBULEUSE_ERROR_NETWORK, "Network error, server unreachable : " + e.ToString()); 
                        }, null);
                        
                    }
                }).Start();
            }
        }
    }
}