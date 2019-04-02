using System;
using Grpcs.Gateway.Wechat;
using StackExchange.Redis;
using System.Threading.Tasks;
using Grpc.Core;
using System.Collections.Generic;
using Hangfire;
using Wechat.Background;
using System.Threading;
using Wechat.Helpers;
using System.Linq;
using Google.Protobuf.Collections;
using Wechat.Api;

namespace Wechat.Grpcs
{
    public class MessageImpl : Message.MessageBase
    {
        MessageQueue _messageQueue { get; }
        EventQueue _eventQueue { get; }
        IDatabase _redis { get; }

        public MessageImpl(MessageQueue messageQueue, EventQueue eventQueue, IDatabase redis)
        {
            _messageQueue = messageQueue;
            _eventQueue = eventQueue;
            _redis = redis;
        }

        public override async Task GetEvent(Empty request, IServerStreamWriter<MessageResponse> responseStream, ServerCallContext context)
        {
            while (true)
            {
                var msg = _eventQueue.Dequeue();
                if (msg == null)
                {
                    Thread.Sleep(500);
                }
                else
                {
                    var resp = new MessageResponse
                    {
                        Error = null,
                        AppId = msg.AppId,
                        Type = msg.Event,
                        OpenId = msg.FromUserName,
                        Time = msg.CreateTime,
                        Content = msg.EventKey
                    };
                    await responseStream.WriteAsync(resp);
                }
            }
        }

        public override async Task GetUser(Empty request, IServerStreamWriter<MessageResponse> responseStream, ServerCallContext context)
        {
            while (true)
            {
                var msg = _messageQueue.Dequeue();
                if (msg == null)
                {
                    Thread.Sleep(500);
                }
                else
                {
                    var resp = new MessageResponse
                    {
                        Error = null,
                        AppId = msg.AppId,
                        Type = msg.MsgType,
                        OpenId = msg.FromUserName,
                        Time = msg.CreateTime
                    };
                    switch (msg.MsgType)
                    {
                        case "image":
                            resp.Content = msg.PicUrl;
                            break;
                        case "text":
                            resp.Content = msg.Content;
                            break;
                        case "link":
                            resp.Content = msg.Url;
                            break;
                        case "location":
                            resp.Content = string.Format("{0},{1}", msg.Location_X, msg.Location_Y);
                            break;
                        default:
                            resp.Content = msg.MediaId;
                            break;
                    }
                    await responseStream.WriteAsync(resp);
                }
            }
        }

        public override Task<MessageStatusResponse> GetStatus(MessageStatusRequest request, ServerCallContext context)
        {
            var resp = new MessageStatusResponse();
            var info = new MessageStatus(_redis, request.MessageId).GetInfo();
            if (info == null)
            {
                resp.Error = new Error
                {
                    ErrCode = 404,
                    ErrMsg = "invalid MessageId"
                };
            }
            else
            {
                var pendingNum = info.PendingList.Count();
                var sendedNum = info.SendedList.Count();
                if (pendingNum == 0 && sendedNum == 0)
                {
                    resp.Status = MessageStatusResponse.Types.Status.Finished;
                }
                else if (pendingNum < info.Total)
                {
                    resp.Status = MessageStatusResponse.Types.Status.Processing;
                }
                else
                {
                    resp.Status = MessageStatusResponse.Types.Status.Pending;
                }
                resp.TotalNum = info.Total;
                resp.SuccessNum = info.SuccessList.Count();
                resp.UserBlockNum = info.UserBlockList.Count();
                resp.SystemFailedNum = info.SystemFailedList.Count();
                resp.SendErrorNum = info.SendErrorList.Count();
                resp.SendTime = info.Time;
                if (request.Detail)
                {
                    resp.SuccessList.AddRange(info.SuccessList);
                    resp.UserBlockList.AddRange(info.UserBlockList);
                    resp.SystemFailedList.AddRange(info.SystemFailedList);
                    resp.SendErrorList.AddRange(info.SendErrorList);
                }
            }
            return Task.FromResult(resp);
        }

        /// <summary>
        /// 处理发送目标
        /// </summary>
        /// <returns>The target.</returns>
        /// <param name="targets">Targets.</param>
        private Dictionary<string, Dictionary<string, string>> _processTarget(RepeatedField<TargetItem> targets)
        {
            var results = new Dictionary<string, Dictionary<string, string>>();
            foreach (var item in targets)
            {
                var replacer = new Dictionary<string, string>();
                if (item.Data != null)
                {
                    foreach (var tmpItem in item.Data)
                    {
                        if (!replacer.ContainsKey(tmpItem.Key))
                        {
                            replacer.Add(tmpItem.Key, tmpItem.Value);
                        }
                    }
                }
                if (!results.ContainsKey(item.OpenId))
                {
                    results.Add(item.OpenId, replacer);
                }
            }
            return results;
        }

        private string _processValue(Dictionary<string, string> replacer, string data)
        {
            foreach (var rItem in replacer)
            {
                data = data.Replace(rItem.Key, rItem.Value);
            }
            return data;
        }

        public override Task<SendMessageResponse> SendText(SendTextRequest request, ServerCallContext context)
        {
            var targets = _processTarget(request.Targets);
            var messageStatus = new MessageStatus(_redis);
            messageStatus.GenMessageId();
            var now = Util.GetTimestamp();
            var resp = new SendMessageResponse
            {
                MessageId = messageStatus.GetMessageId(),
                SendTime = request.Time == 0 ? now : request.Time
            };
            if (request.NoStatus)
            {
                resp.MessageId = string.Empty;
                messageStatus.SetMessageId(null);
            }
            else
            {
                messageStatus.Create(targets.Keys.ToArray(), resp.SendTime);
            }
            foreach (var item in targets)
            {
                var tmpContent = _processValue(item.Value, request.Content);
                if (request.Time == 0)
                {
                    if (request.HighPriority)
                    {

                        BackgroundJob.Enqueue<SendMessage>(x => x.HighPriorityText(messageStatus.GetMessageId(), request.AppId, item.Key, tmpContent, null));
                    }
                    else
                    {
                        BackgroundJob.Enqueue<SendMessage>(x => x.Text(messageStatus.GetMessageId(), request.AppId, item.Key, tmpContent, null));
                    }
                }
                else
                {
                    var tmpJobId = string.Empty;
                    if (request.HighPriority)
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.HighPriorityText(messageStatus.GetMessageId(), request.AppId, item.Key, tmpContent, null), TimeSpan.FromSeconds(request.Time - now));
                    }
                    else
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.Text(messageStatus.GetMessageId(), request.AppId, item.Key, tmpContent, null), TimeSpan.FromSeconds(request.Time - now));
                    }
                    messageStatus.AddJobId(tmpJobId);
                }
            }
            return Task.FromResult(resp);
        }

        public override Task<SendMessageResponse> SendImage(SendImageRequest request, ServerCallContext context)
        {
            var targets = _processTarget(request.Targets);
            var messageStatus = new MessageStatus(_redis);
            messageStatus.GenMessageId();
            var now = Util.GetTimestamp();
            var resp = new SendMessageResponse
            {
                MessageId = messageStatus.GetMessageId(),
                SendTime = request.Time == 0 ? now : request.Time
            };
            if (request.NoStatus)
            {
                resp.MessageId = string.Empty;
                messageStatus.SetMessageId(null);
            }
            else
            {
                messageStatus.Create(targets.Keys.ToArray(), resp.SendTime);
            }
            foreach (var item in targets)
            {
                if (request.Time == 0)
                {
                    if (request.HighPriority)
                    {

                        BackgroundJob.Enqueue<SendMessage>(x => x.HighPriorityImage(messageStatus.GetMessageId(), request.AppId, item.Key, request.ImageId, null));
                    }
                    else
                    {
                        BackgroundJob.Enqueue<SendMessage>(x => x.Image(messageStatus.GetMessageId(), request.AppId, item.Key, request.ImageId, null));
                    }
                }
                else
                {
                    var tmpJobId = string.Empty;
                    if (request.HighPriority)
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.HighPriorityImage(messageStatus.GetMessageId(), request.AppId, item.Key, request.ImageId, null), TimeSpan.FromSeconds(request.Time - now));
                    }
                    else
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.Image(messageStatus.GetMessageId(), request.AppId, item.Key, request.ImageId, null), TimeSpan.FromSeconds(request.Time - now));
                    }
                    messageStatus.AddJobId(tmpJobId);
                }
            }
            return Task.FromResult(resp);
        }

        public override Task<SendMessageResponse> SendNews(SendNewsRequest request, ServerCallContext context)
        {
            var isWxApp = _redis.KeyExists(CacheKey.UserIsWxAppPrefix + request.AppId);
            var targets = _processTarget(request.Targets);
            var messageStatus = new MessageStatus(_redis);
            messageStatus.GenMessageId();
            var now = Util.GetTimestamp();
            var resp = new SendMessageResponse
            {
                MessageId = messageStatus.GetMessageId(),
                SendTime = request.Time == 0 ? now : request.Time
            };
            if (request.NoStatus)
            {
                resp.MessageId = string.Empty;
                messageStatus.SetMessageId(null);
            }
            else
            {
                messageStatus.Create(targets.Keys.ToArray(), resp.SendTime);
            }
            foreach (var item in targets)
            {
                var tmpTitle = _processValue(item.Value, request.Title);
                var tmpDescription = _processValue(item.Value, request.Description);
                var tmpLink = _processValue(item.Value, request.Link);
                if (request.Time == 0)
                {
                    if (request.HighPriority)
                    {
                        BackgroundJob.Enqueue<SendMessage>(x => x.HighPriorityNews(messageStatus.GetMessageId(), request.AppId, item.Key, tmpTitle, tmpDescription, tmpLink, request.Image, isWxApp, null));
                    }
                    else
                    {
                        BackgroundJob.Enqueue<SendMessage>(x => x.News(messageStatus.GetMessageId(), request.AppId, item.Key, tmpTitle, tmpDescription, tmpLink, request.Image, isWxApp, null));
                    }
                }
                else
                {
                    var tmpJobId = string.Empty;
                    if (request.HighPriority)
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.HighPriorityNews(messageStatus.GetMessageId(), request.AppId, item.Key, tmpTitle, tmpDescription, tmpLink, request.Image, isWxApp, null), TimeSpan.FromSeconds(request.Time - now));
                    }
                    else
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.News(messageStatus.GetMessageId(), request.AppId, item.Key, tmpTitle, tmpDescription, tmpLink, request.Image, isWxApp, null), TimeSpan.FromSeconds(request.Time - now));

                    }
                    messageStatus.AddJobId(tmpJobId);
                }
            }
            return Task.FromResult(resp);
        }

        public override Task<SendMessageResponse> SendWxAppCard(SendWxAppCardRequest request, ServerCallContext context)
        {
            var targets = _processTarget(request.Targets);
            var messageStatus = new MessageStatus(_redis);
            messageStatus.GenMessageId();
            var now = Util.GetTimestamp();
            var resp = new SendMessageResponse
            {
                MessageId = messageStatus.GetMessageId(),
                SendTime = request.Time == 0 ? now : request.Time
            };
            if (request.NoStatus)
            {
                resp.MessageId = string.Empty;
                messageStatus.SetMessageId(null);
            }
            else
            {
                messageStatus.Create(targets.Keys.ToArray(), resp.SendTime);
            }
            foreach (var item in targets)
            {
                var tmpTitle = _processValue(item.Value, request.Title);
                var tmpPagePath = _processValue(item.Value, request.PagePath);
                if (request.Time == 0)
                {
                    if (request.HighPriority)
                    {
                        BackgroundJob.Enqueue<SendMessage>(x => x.HighPriorityWxAppCard(messageStatus.GetMessageId(), request.AppId, item.Key, tmpTitle, tmpPagePath, request.WxAppId, request.ImageId, null));
                    }
                    else
                    {
                        BackgroundJob.Enqueue<SendMessage>(x => x.WxAppCard(messageStatus.GetMessageId(), request.AppId, item.Key, tmpTitle, tmpPagePath, request.WxAppId, request.ImageId, null));
                    }
                }
                else
                {
                    var tmpJobId = string.Empty;
                    if (request.HighPriority)
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.HighPriorityWxAppCard(messageStatus.GetMessageId(), request.AppId, item.Key, tmpTitle, tmpPagePath, request.WxAppId, request.ImageId, null), TimeSpan.FromSeconds(request.Time - now));
                    }
                    else
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.WxAppCard(messageStatus.GetMessageId(), request.AppId, item.Key, tmpTitle, tmpPagePath, request.WxAppId, request.ImageId, null), TimeSpan.FromSeconds(request.Time - now));
                    }
                    messageStatus.AddJobId(tmpJobId);
                }
            }
            return Task.FromResult(resp);
        }

        public override Task<GetTemplateResponse> GetTemplate(GetInfoRequest request, ServerCallContext context)
        {
            var resp = new GetTemplateResponse();
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + request.AppId);
            if (accessToken.HasValue)
            {
                var isWxApp = _redis.KeyExists(CacheKey.UserIsWxAppPrefix + request.AppId);
                if (isWxApp)
                {
                    var data = WxAppApi.GetTemplateList(accessToken);
                    if (data.ErrCode != 0)
                    {
                        resp.Error = new Error
                        {
                            ErrCode = data.ErrCode,
                            ErrMsg = data.ErrMsg
                        };
                    }
                    else
                    {
                        foreach (var item in data.TemplateList)
                        {
                            resp.TemplateList.Add(new GetTemplateResponse.Types.ListItem
                            {
                                TemplateId = item.TemplateId,
                                Title = item.Title,
                                Content = item.Content,
                                Example = item.Example
                            });
                        }
                    }
                }
                else
                {
                    var data = WxWebApi.GetTemplateList(accessToken);
                    if (data.ErrCode != 0)
                    {
                        resp.Error = new Error
                        {
                            ErrCode = data.ErrCode,
                            ErrMsg = data.ErrMsg
                        };
                    }
                    else
                    {
                        foreach (var item in data.TemplateList)
                        {
                            resp.TemplateList.Add(new GetTemplateResponse.Types.ListItem
                            {
                                TemplateId = item.TemplateId,
                                Title = item.Title,
                                PrimaryIndustry = item.PrimaryIndustry,
                                DeputyIndustry = item.DeputyIndustry,
                                Content = item.Content,
                                Example = item.Example
                            });
                        }
                    }
                }

            }
            else
            {
                resp.Error = new Error
                {
                    ErrCode = 99999,
                    ErrMsg = "AccessToken Missing"
                };
            }
            return Task.FromResult(resp);
        }

        public override Task<SendMessageResponse> SendTemplate(SendTemplateRequest request, ServerCallContext context)
        {
            string formId = null;
            if (_redis.KeyExists(CacheKey.UserIsWxAppPrefix + request.AppId))
            {
                formId = request.FormId;
            }
            var targets = _processTarget(request.Targets);
            var messageStatus = new MessageStatus(_redis);
            messageStatus.GenMessageId();
            var now = Util.GetTimestamp();
            var resp = new SendMessageResponse
            {
                MessageId = messageStatus.GetMessageId(),
                SendTime = request.Time == 0 ? now : request.Time
            };
            if (request.NoStatus)
            {
                resp.MessageId = string.Empty;
                messageStatus.SetMessageId(null);
            }
            else
            {
                messageStatus.Create(targets.Keys.ToArray(), resp.SendTime);
            }
            foreach (var item in targets)
            {
                var data = new Dictionary<string, WxWebSendTemplateRequest.DataItem>();
                foreach (var dItem in request.Data)
                {
                    data.Add(dItem.Key, new WxWebSendTemplateRequest.DataItem
                    {
                        Value = _processValue(item.Value, dItem.Value),
                        Color = dItem.Color
                    });
                }
                var tmpUrl = _processValue(item.Value, request.Url);
                if (request.Time == 0)
                {
                    if (request.HighPriority)
                    {
                        BackgroundJob.Enqueue<SendMessage>(x => x.HighPriorityTemplate(messageStatus.GetMessageId(), request.AppId, item.Key, request.TemplateId, tmpUrl, data, formId, null));
                    }
                    else
                    {
                        BackgroundJob.Enqueue<SendMessage>(x => x.Template(messageStatus.GetMessageId(), request.AppId, item.Key, request.TemplateId, tmpUrl, data, formId, null));
                    }
                }
                else
                {
                    var tmpJobId = string.Empty;
                    if (request.HighPriority)
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.HighPriorityTemplate(messageStatus.GetMessageId(), request.AppId, item.Key, request.TemplateId, tmpUrl, data, formId, null), TimeSpan.FromSeconds(request.Time - now));
                    }
                    else
                    {
                        tmpJobId = BackgroundJob.Schedule<SendMessage>(x => x.Template(messageStatus.GetMessageId(), request.AppId, item.Key, request.TemplateId, tmpUrl, data, formId, null), TimeSpan.FromSeconds(request.Time - now));
                    }
                    messageStatus.AddJobId(tmpJobId);
                }
            }
            return Task.FromResult(resp);
        }

        public override Task<Error> Cancel(MessageStatusRequest request, ServerCallContext context)
        {
            var resp = new Error();
            if (string.IsNullOrEmpty(request.MessageId))
            {
                resp.ErrCode = -1;
                resp.ErrMsg = "Wrong message id";
            }
            else
            {
                var messageStatus = new MessageStatus(_redis, request.MessageId);
                if (messageStatus.GetInfo().Time > Util.GetTimestamp())
                {
                    BackgroundJob.Enqueue<MessageStatus>(x => x.CancelJob(messageStatus.GetMessageId(), null));
                }
                else
                {
                    resp.ErrCode = 9999;
                    resp.ErrMsg = "Can't Cancel, already processed!";
                }
            }
            return Task.FromResult(resp);
        }


    }
}
