using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Tencent;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Hangfire;
using Wechat.Background;
using Wechat.Helpers;

namespace Wechat.Web
{
    /// <summary>
    /// 微信推送接收.
    /// </summary>
    public class HttpController : ControllerBase
    {
        IConfiguration _config { get; }
        WXBizMsgCrypt _weSdk { get; }
        ILogger _logger { get; }
        IDatabase _redis { get; set; }
        MessageQueue _messageQueue { get; }
        EventQueue _eventQueue { get; }

        string _componentAppId
        {
            get
            {
                return _config["Wechat:AppID"];
            }
        }
        string _componentAccessToken
        {
            get
            {
                return _redis.StringGet(CacheKey.ComponentAccessToken);
            }
        }

        public HttpController(IConfiguration config, WXBizMsgCrypt wx, IDatabase redis, ILogger<HttpController> logger,
                              MessageQueue messageQueue, EventQueue eventQueue)
        {
            _logger = logger;
            _config = config;
            _weSdk = wx;
            _redis = redis;
            _messageQueue = messageQueue;
            _eventQueue = eventQueue;
        }

        /// <summary>
        /// 平台消息推送
        /// </summary>
        /// <returns>The ticket handler.</returns>
        [HttpPost("platform")]
        public string PlatformMessageHandler([FromQuery]MessageRequestQuery query, [FromBody]MessageRequsetBody body)
        {
            if (_weSdk.CheckMsgSign(query, body.Encrypt))
            {
                var data = new PlatformMessageRequestXml(_weSdk.DecryptMsg(body.Encrypt));
                switch (data.InfoType)
                {
                    case "updateauthorized":
                        BackgroundJob.Enqueue<UpdateAuth>(x => x.Run(data.AuthorizationCode, null));
                        break;
                    case "unauthorized":
                        BackgroundJob.Enqueue<ClearAuth>(x => x.Run(data.AuthorizerAppId, null));
                        break;
                    case "authorized":
                        BackgroundJob.Enqueue<UpdateAuth>(x => x.Run(data.AuthorizationCode, null));
                        break;
                    case "component_verify_ticket":
                        BackgroundJob.Enqueue<UpdateVerifyTicket>(x => x.Run(data.ComponentVerifyTicket, null));
                        break;
                }
            }
            return "success";
        }

        /// <summary>
        /// 会员号消息推送
        /// </summary>
        /// <returns>The message handler.</returns>
        [HttpPost("user/{appid}")]
        public string UserMessageHandler(string appid, [FromQuery]MessageRequestQuery query, [FromBody]MessageRequsetBody body)
        {
            if (_weSdk.CheckMsgSign(query, body.Encrypt))
            {
                var data = new UserMessageRequsetXml(_weSdk.DecryptMsg(body.Encrypt))
                {
                    AppId = appid
                };
                if (data.MsgType == "event")
                {
                    if (data.Event == "TEMPLATESENDJOBFINISH")
                    {
                        var messageStatus = new MessageStatus(_redis);
                        var messageId = messageStatus.GetTemplateMessageId(data.MsgId);
                        if (messageId != null)
                        {
                            messageStatus.SetMessageId(messageId);
                            switch (data.Status)
                            {
                                case TemplateMessageStatus.Success:
                                    messageStatus.Success(data.FromUserName);
                                    break;
                                case TemplateMessageStatus.UserBlock:
                                    messageStatus.UserBlock(data.FromUserName);
                                    break;
                                case TemplateMessageStatus.SystemFailed:
                                    messageStatus.SystemFailed(data.FromUserName);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        _eventQueue.Enqueue(data);
                        if (data.Event == "weapp_audit_success")
                        {
                            //TODO:: For Audit Success Auto Process;
                        }
                    }
                }
                else
                {
                    _messageQueue.Enqueue(data);
                }
            }
            return "success";
        }

        [HttpGet("")]
        public IActionResult GotoDashboard()
        {
            return _config["Hangfire:Path"] == "/" ? (IActionResult)Redirect("/dashboard") : NotFound();
        }
    }
}