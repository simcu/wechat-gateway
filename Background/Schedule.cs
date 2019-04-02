using System;
using Hangfire;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Wechat.Api;
using Microsoft.AspNetCore.Builder;
using Hangfire.Server;
using Hangfire.Console;
using System.Linq;
using Wechat.Helpers;

namespace Wechat.Background
{
    /// <summary>
    /// 注册计划任务
    /// </summary>
    public static class BackgroundScheduleMiddlewareExtensions
    {
        public static IApplicationBuilder UseBackgroundSchedule(this IApplicationBuilder builder)
        {
            RecurringJob.AddOrUpdate<KeyGuard>(x => x.ComponetAccessToken(null), Cron.Minutely);
            RecurringJob.AddOrUpdate<KeyGuard>(x => x.UserAccessToken(null), Cron.Minutely);
            RecurringJob.AddOrUpdate<KeyGuard>(x => x.UserJsTicket(null), Cron.Minutely);
            return builder;
        }
    }

    /// <summary>
    /// 检查和保证AccessToken存在
    /// </summary>
    public class KeyGuard
    {
        IDatabase _redis { get; }
        ILogger _logger { get; }
        IConfiguration _config { get; }

        public KeyGuard(IDatabase database, IConfiguration configuration)
        {
            _config = configuration;
            _redis = database;
        }

        /// <summary>
        /// 监控平台AccessToken
        /// </summary>
        [Queue("schedule"), AutomaticRetry(Attempts = 0)]
        public void ComponetAccessToken(PerformContext context = null)
        {
            context.WriteLine("开始检查ComponetAccessToken...");
            if (!_redis.KeyExists(CacheKey.ComponentAccessToken) || _redis.KeyTimeToLive(CacheKey.ComponentAccessToken) < new TimeSpan(0, 30, 0))
            {
                context.WriteLine("需要更新ComponetAccessToken...");
                if (_redis.KeyExists(CacheKey.ComponentVerifyTicket))
                {
                    context.WriteLine("开始更新ComponetAccessToken...");
                    var resp = ComponentApi.GetComponentAccessToken(_config["Wechat:AppID"], _config["Wechat:AppSecret"], _redis.StringGet(CacheKey.ComponentVerifyTicket));
                    if (resp.ErrCode == 0)
                    {
                        _redis.StringSet(CacheKey.ComponentAccessToken, resp.ComponentAccessToken, new TimeSpan(0, 0, resp.ExpiresIn));
                        context.WriteLine("更新ComponetAccessToken成功...");
                    }
                    else
                    {
                        context.WriteLine("更新ComponetAccessToken发生错误：" + resp.ErrMsg);
                        throw new ServiceException(resp.ErrCode, resp.ErrMsg);
                    }
                }
                else
                {
                    context.WriteLine("更新ComponetAccessToken发生错误：ComponentVerifyTicket不存在...");
                    throw new ServiceException(-1, "更新ComponetAccessToken发生错误：ComponentVerifyTicket不存在...");
                }
            }
            else
            {
                context.WriteLine("检查ComponetAccessToken完毕...");
            }
        }

        /// <summary>
        /// 监控代理的AccessToken
        /// </summary>
        /// <param name="context">Context.</param>
        [Queue("schedule"), AutomaticRetry(Attempts = 0)]
        public void UserAccessToken(PerformContext context = null)
        {
            var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
            context.WriteLine("开始检查用户AccessToken ...");
            foreach (var key in server.Keys(database: int.Parse(_config["Redis:CredentialDb"]), pattern: CacheKey.UserRefreshTokenPrefix + "*"))
            {
                var tmpId = key.ToString().Replace(CacheKey.UserRefreshTokenPrefix, string.Empty);
                if (!_redis.KeyExists(CacheKey.UserAccessTokenPrefix + tmpId) || _redis.KeyTimeToLive(CacheKey.UserAccessTokenPrefix + tmpId) < new TimeSpan(0, 30, 0))
                {
                    BackgroundJob.Enqueue<RefreshAccessToken>(x => x.Run(tmpId, null));
                    context.WriteLine("【{0}】AccessToken需要更新，创建更新任务...", tmpId);
                }
                else
                {
                    context.WriteLine("【{0}】AccessToken状态正常，跳过...", tmpId);
                }
            }
            context.WriteLine("检查用户AccessToken完毕 ...");
        }

        [Queue("schedule"), AutomaticRetry(Attempts = 0)]
        public void UserJsTicket(PerformContext context = null)
        {
            var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
            context.WriteLine("开始检查用户JsTicket ...");
            foreach (var key in server.Keys(database: int.Parse(_config["Redis:CredentialDb"]), pattern: CacheKey.UserAccessTokenPrefix + "*"))
            {
                var tmpId = key.ToString().Replace(CacheKey.UserAccessTokenPrefix, string.Empty);
                if (_redis.KeyExists(CacheKey.UserIsWxAppPrefix + tmpId))
                {
                    context.WriteLine("【{0}】是小程序，跳过...", tmpId);
                }
                else
                {
                    if (!_redis.KeyExists(CacheKey.UserJsTicketPrefix + tmpId) || _redis.KeyTimeToLive(CacheKey.UserJsTicketPrefix + tmpId) < new TimeSpan(0, 30, 0))
                    {
                        BackgroundJob.Enqueue<RefreshJsTicket>(x => x.Run(tmpId, null));
                        context.WriteLine("【{0}】JsTicket需要更新，创建更新任务...", tmpId);
                    }
                    else
                    {
                        context.WriteLine("【{0}】JsTicket状态正常，跳过...", tmpId);
                    }
                }
            }
            context.WriteLine("检查用户JsTicket完毕 ...");
        }
    }

}
