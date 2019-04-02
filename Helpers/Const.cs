using System;
using Wechat.Web;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Wechat.Helpers
{
    /// <summary>
    /// Redis缓存Key定义
    /// </summary>
    public struct CacheKey
    {
        public const string ComponentVerifyTicket = "{wechat}:component:verify-ticket";
        public const string ComponentAccessToken = "{wechat}:component:access-token";
        public const string UserAccessTokenPrefix = "{wechat}:user:access-token:";
        public const string UserRefreshTokenPrefix = "{wechat}:user:refresh-token:";
        public const string UserJsTicketPrefix = "{wechat}:user:js-ticket:";
        public const string EventQueue = "{wechat}:queue:event";
        public const string MessageQueue = "{wechat}:queue:message";
        public const string HangfireDashboardAuthPrefix = "{hangfire}:dashboard:auth:";
        public const string MessageStatus = "{wechat}:message-status:";
        public const string MessageJobIdMapPrefix = "{wechat}:message-job-map:";
        public const string MessageCleanerIdPrefix = "{wechat}:message-clean-job-id:";
        public const string UserIsWxAppPrefix = "{wechat}:user:wxapp:";
    }

    /// <summary>
    /// 微信接口异常定义
    /// </summary>
    public class ServiceException : ApplicationException
    {
        public int ErrCode { get; set; }
        public ServiceException(int code, string message) : base(message)
        {
            ErrCode = code;
        }
    }

}
