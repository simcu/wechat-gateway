using Hangfire.Server;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Hangfire.Console;
using Wechat.Web;
using System;
using Hangfire;
using System.Collections.Generic;
using Wechat.Api;
using Wechat.Helpers;

namespace Wechat.Background
{

    /// <summary>
    /// 刷新AccessToken
    /// </summary>
    public class RefreshAccessToken
    {
        IDatabase _redis { get; }
        IConfiguration _config { get; }
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

        public RefreshAccessToken(IDatabase redis, IConfiguration config)
        {
            _redis = redis;
            _config = config;
        }

        [Queue("platform"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Run(string appId, PerformContext context = null)
        {
            context.WriteLine("开始刷新AccessToken【{0}】...", appId);
            var resp = ComponentApi.RefreshAccessToken(_componentAccessToken, _componentAppId, appId, _redis.StringGet(CacheKey.UserRefreshTokenPrefix + appId));
            if (resp.ErrCode == 0)
            {
                _redis.StringSet(CacheKey.UserAccessTokenPrefix + appId, resp.AuthorizerAccessToken, new TimeSpan(0, 0, resp.ExpiresIn));
                _redis.StringSet(CacheKey.UserRefreshTokenPrefix + appId, resp.AuthorizerRefreshToken);
                context.WriteLine("刷新AccessToken【{0}】成功...", appId);
            }
            else
            {
                context.WriteLine("刷新AccessToken【{0}】失败：{1}...", appId, resp.ErrMsg);
                throw new ServiceException(resp.ErrCode, resp.ErrMsg);
            }
        }
    }

    /// <summary>
    /// 刷新JsTicket
    /// </summary>
    public class RefreshJsTicket
    {
        IDatabase _redis { get; }

        public RefreshJsTicket(IDatabase redis)
        {
            _redis = redis;
        }

        [Queue("platform"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Run(string appId, PerformContext context = null)
        {
            context.WriteLine("开始刷新JsTicket【{0}】...", appId);
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + appId);
            if (accessToken.HasValue)
            {
                var resp = WxWebApi.GetJsTicket(accessToken);
                if (resp.ErrCode == 0)
                {
                    _redis.StringSet(CacheKey.UserJsTicketPrefix + appId, resp.Ticket, new TimeSpan(0, 0, resp.ExpiresIn));
                    context.WriteLine("刷新JsTicket【{0}】成功...", appId);
                }
                else
                {
                    context.WriteLine("刷新JsTicket【{0}】失败：{1}...", appId, resp.ErrMsg);
                    throw new ServiceException(resp.ErrCode, resp.ErrMsg);
                }
            }
            else
            {
                context.WriteLine("刷新JsTicket失败：无法获取AccessToken...");
                throw new ServiceException(-1, "刷新JsTicket失败：无法获取AccessToken...");
            }

        }
    }

    /// <summary>
    /// 新增和更新代理授权
    /// </summary>
    public class UpdateAuth
    {
        IDatabase _redis { get; }
        IConfiguration _config { get; }
        EventQueue _event { get; }
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

        public UpdateAuth(IDatabase redis, IConfiguration config, EventQueue eventQueue)
        {
            _redis = redis;
            _config = config;
            _event = eventQueue;
        }

        [Queue("platform"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Run(string code, PerformContext context)
        {
            context.WriteLine("开始处理新增和更新代理授权【Code: {0}】...", code);
            var authInfo = ComponentApi.GetAuthInfo(_componentAccessToken, _componentAppId, code);
            if (authInfo.ErrCode == 0)
            {
                _redis.StringSet(CacheKey.UserAccessTokenPrefix + authInfo.AuthorizationInfo.AuthorizerAppId, authInfo.AuthorizationInfo.AuthorizerAccessToken, new TimeSpan(0, 0, authInfo.AuthorizationInfo.ExpiresIn));
                _redis.StringSet(CacheKey.UserRefreshTokenPrefix + authInfo.AuthorizationInfo.AuthorizerAppId, authInfo.AuthorizationInfo.AuthorizerRefreshToken);
                var now = (DateTime.UtcNow.Ticks - 621355968000000000) / 10000000;
                var message = new UserMessageRequsetXml("<xml></xml>")
                {
                    AppId = authInfo.AuthorizationInfo.AuthorizerAppId,
                    MsgType = "event",
                    Event = "authorized",
                    FromUserName = authInfo.AuthorizationInfo.AuthorizerAppId,
                    EventKey = code,
                    CreateTime = (int)now
                };
                _event.Enqueue(message);
                context.WriteLine("创建授权事件到推送...");
                context.WriteLine("处理新增和更新代理授权【{0}】完毕...", authInfo.AuthorizationInfo.AuthorizerAppId);
            }
            else
            {
                context.WriteLine("处理新增和更新代理授权【{0}】错误：{1}...", authInfo.AuthorizationInfo.AuthorizerAppId, authInfo.ErrMsg);
                throw new ServiceException(authInfo.ErrCode, authInfo.ErrMsg);
            }
        }
    }

    /// <summary>
    /// 取消授权代理清理
    /// </summary>
    public class ClearAuth
    {
        IDatabase _redis { get; }
        IConfiguration _config { get; }
        EventQueue _event { get; }
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

        public ClearAuth(IDatabase redis, IConfiguration config, EventQueue eventQueue)
        {
            _redis = redis;
            _config = config;
            _event = eventQueue;
        }

        [Queue("platform"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Run(string appId, PerformContext context)
        {
            context.WriteLine("开始清理代理授权【{0}】...", appId);
            _redis.KeyDelete(CacheKey.UserAccessTokenPrefix + appId);
            context.WriteLine("清理AccessToken完毕...");
            _redis.KeyDelete(CacheKey.UserRefreshTokenPrefix + appId);
            context.WriteLine("清理RefreshToken完毕...");
            var now = (DateTime.UtcNow.Ticks - 621355968000000000) / 10000000;
            var message = new UserMessageRequsetXml("<xml></xml>")
            {
                AppId = appId,
                MsgType = "event",
                Event = "unauthorized",
                FromUserName = appId,
                EventKey = "unauthorized",
                CreateTime = (int)now
            };
            _event.Enqueue(message);
            context.WriteLine("创建取消授权事件...");
            context.WriteLine("清理代理授权【{0}】完毕...", appId);
        }
    }

    /// <summary>
    /// 更新VerifyTicket
    /// </summary>
    public class UpdateVerifyTicket
    {
        IDatabase _redis { get; }
        public UpdateVerifyTicket(IDatabase redis)
        {
            _redis = redis;
        }

        [Queue("platform"), AutomaticRetry(Attempts = 0)]
        public void Run(string ticket, PerformContext context)
        {
            context.WriteLine("开始处理平台VerifyTicket...");
            if (!string.IsNullOrEmpty(ticket))
            {
                _redis.StringSet(CacheKey.ComponentVerifyTicket, ticket);
                context.WriteLine("更新VerifyTicket为{0}...", ticket);
            }
            else
            {
                context.WriteLine("获取的VerifyTicket为空，更新失败...");
                throw new ServiceException(-1, "获取的VerifyTicket为空，更新失败...");
            }
        }
    }

    /// <summary>
    /// 发送信息给用户
    /// </summary>
    public class SendMessage
    {
        IDatabase _redis { get; }
        public SendMessage(IDatabase redis)
        {
            _redis = redis;
        }

        [Queue("message"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Text(string messageId, string appId, string openId, string content, PerformContext context)
        {
            var messageStatus = new MessageStatus(_redis, messageId);
            messageStatus.Sended(openId);
            context.WriteLine("向用户「{0}」@「{1}」发送文本消息...", openId, appId);
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + appId);
            if (accessToken.HasValue)
            {
                var resp = MessageApi.SendText(accessToken, openId, content);
                if (resp.ErrCode == 0)
                {
                    messageStatus.Success(openId);
                    context.WriteLine("消息发送成功...");
                }
                else
                {
                    messageStatus.SendError(openId);
                    context.WriteLine("消息发送失败：{0}...", resp.ErrMsg);
                    throw new ServiceException(resp.ErrCode, resp.ErrMsg);
                }
            }
            else
            {
                messageStatus.SendError(openId);
                context.WriteLine("消息发送失败：无法获取AccessToken...");
                throw new ServiceException(-1, "消息发送失败：无法获取AccessToken...");
            }
        }

        [Queue("message_high"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void HighPriorityText(string messageId, string appId, string openId, string content, PerformContext context)
        {
            Text(messageId, appId, openId, content, context);
        }

        [Queue("message"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void News(string messageId, string appId, string openId, string title, string description, string url, string picUrl, bool isWxApp, PerformContext context)
        {
            var messageStatus = new MessageStatus(_redis, messageId);
            messageStatus.Sended(openId);
            context.WriteLine("向用户「{0}」@「{1}」发送图文消息...", openId, appId);
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + appId);
            if (accessToken.HasValue)
            {
                var resp = MessageApi.SendNews(accessToken, openId, title, description, url, picUrl, isWxApp);
                if (resp.ErrCode == 0)
                {
                    messageStatus.Success(openId);
                    context.WriteLine("消息发送成功...");
                }
                else
                {
                    messageStatus.SendError(openId);
                    context.WriteLine("消息发送失败：{0}...", resp.ErrMsg);
                    throw new ServiceException(resp.ErrCode, resp.ErrMsg);
                }
            }
            else
            {
                messageStatus.SendError(openId);
                context.WriteLine("消息发送失败：无法获取AccessToken...");
                throw new ServiceException(-1, "消息发送失败：无法获取AccessToken...");
            }
        }

        [Queue("message_high"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void HighPriorityNews(string messageId, string appId, string openId, string title, string description, string url, string picUrl, bool isWxApp, PerformContext context)
        {
            News(messageId, appId, openId, title, description, url, picUrl, isWxApp, context);
        }

        [Queue("message"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Template(string messageId, string appId, string openId, string templateId, string url, Dictionary<string, WxWebSendTemplateRequest.DataItem> data, string formId, PerformContext context)
        {
            var messageStatus = new MessageStatus(_redis, messageId);
            messageStatus.Sended(openId);
            context.WriteLine("向用户「{0}」@「{1}」发送模版消息「{2}」...", openId, appId, templateId);
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + appId);
            if (accessToken.HasValue)
            {
                if (string.IsNullOrEmpty(formId))

                {
                    var resp = WxWebApi.SendTemplate(accessToken, openId, templateId, url, data);
                    if (resp.ErrCode == 0)
                    {
                        messageStatus.SetTemplateMap(resp.MsgId.ToString());
                        context.WriteLine("消息发送成功...[MsgId: {0}]", resp.MsgId);
                    }
                    else
                    {
                        messageStatus.SendError(openId);
                        context.WriteLine("消息发送失败：{0}...", resp.ErrMsg);
                        throw new ServiceException(resp.ErrCode, resp.ErrMsg);
                    }
                }
                else
                {
                    var tmpData = new Dictionary<string, WxAppSendTemplateRequest.DataItem>();
                    foreach (var item in data)
                    {
                        tmpData.Add(item.Key, new WxAppSendTemplateRequest.DataItem { Value = item.Value.Value });
                    }
                    var resp = WxAppApi.SendTemplate(accessToken, openId, templateId, url, formId, tmpData);
                    if (resp.ErrCode == 0)
                    {
                        messageStatus.Success(openId);
                        context.WriteLine("消息发送成功...");
                    }
                    else
                    {
                        messageStatus.SendError(openId);
                        context.WriteLine("消息发送失败：{0}...", resp.ErrMsg);
                        throw new ServiceException(resp.ErrCode, resp.ErrMsg);
                    }
                }
            }
            else
            {
                messageStatus.SendError(openId);
                context.WriteLine("消息发送失败：无法获取AccessToken...");
                throw new ServiceException(-1, "消息发送失败：无法获取AccessToken...");
            }
        }

        [Queue("message_high"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void HighPriorityTemplate(string messageId, string appId, string openId, string templateId, string url, Dictionary<string, WxWebSendTemplateRequest.DataItem> data, string formId, PerformContext context)
        {
            Template(messageId, appId, openId, templateId, url, data, formId, context);
        }

        [Queue("message"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Image(string messageId, string appId, string openId, string imageId, PerformContext context = null)
        {
            var messageStatus = new MessageStatus(_redis, messageId);
            messageStatus.Sended(openId);
            context.WriteLine("向用户「{0}」@「{1}」发送图片消息...", openId, appId);
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + appId);
            if (accessToken.HasValue)
            {
                var resp = MessageApi.SendImage(accessToken, openId, imageId);
                if (resp.ErrCode == 0)
                {
                    messageStatus.Success(openId);
                    context.WriteLine("消息发送成功...");
                }
                else
                {
                    messageStatus.SendError(openId);
                    context.WriteLine("消息发送失败：{0}...", resp.ErrMsg);
                    throw new ServiceException(resp.ErrCode, resp.ErrMsg);
                }
            }
            else
            {
                messageStatus.SendError(openId);
                context.WriteLine("消息发送失败：无法获取AccessToken...");
                throw new ServiceException(-1, "消息发送失败：无法获取AccessToken...");
            }
        }

        [Queue("message_high"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void HighPriorityImage(string messageId, string appId, string openId, string imageId, PerformContext context = null)
        {
            Image(messageId, appId, openId, imageId, context);
        }

        [Queue("message"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void WxAppCard(string messageId, string appId, string openId, string title, string pagePath, string wxAppId, string imageId, PerformContext context = null)
        {
            var messageStatus = new MessageStatus(_redis, messageId);
            messageStatus.Sended(openId);
            context.WriteLine("向用户「{0}」@「{1}」发送小程序卡片...", openId, appId);
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + appId);
            if (accessToken.HasValue)
            {
                var resp = MessageApi.SendWxAppCard(accessToken, openId, title, wxAppId, pagePath, imageId);
                if (resp.ErrCode == 0)
                {
                    messageStatus.Success(openId);
                    context.WriteLine("消息发送成功...");
                }
                else
                {
                    messageStatus.SendError(openId);
                    context.WriteLine("消息发送失败：{0}...", resp.ErrMsg);
                    throw new ServiceException(resp.ErrCode, resp.ErrMsg);
                }
            }
            else
            {
                messageStatus.SendError(openId);
                context.WriteLine("消息发送失败：无法获取AccessToken...");
                throw new ServiceException(-1, "消息发送失败：无法获取AccessToken...");
            }
        }

        [Queue("message_high"), AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void HighPriorityWxAppCard(string messageId, string appId, string openId, string title, string pagePath, string wxAppId, string imageId, PerformContext context = null)
        {
            WxAppCard(messageId, appId, openId, title, pagePath, wxAppId, imageId, context);
        }
    }
}
