using System;
using StackExchange.Redis;
using System.Linq;
using Hangfire;
using Hangfire.Server;
using Hangfire.Console;
using Microsoft.Extensions.Logging;
namespace Wechat.Helpers
{
    public class MessageStatus
    {
        private string _messageId { get; set; }
        private IDatabase _redis { get; set; }
        private bool _noStatus { get; set; } = false;

        public MessageStatus(IDatabase redis)
        {
            _redis = redis;
        }

        public MessageStatus(IDatabase redis, string messageId) : this(redis)
        {
            SetMessageId(messageId);
        }

        public class MessageStatusInfo
        {
            public int Total { get; set; }
            public int Time { get; set; }
            public string[] PendingList { get; set; }
            public string[] SendedList { get; set; }
            public string[] SuccessList { get; set; }
            public string[] UserBlockList { get; set; }
            public string[] SystemFailedList { get; set; }
            public string[] SendErrorList { get; set; }

        }

        public void GenMessageId()
        {
            _messageId = Guid.NewGuid().ToString();
        }

        public void SetNoStatus(bool status = true)
        {
            _noStatus = status;
        }

        public void SetMessageId(string messageId)
        {
            _messageId = messageId;
            if (messageId == null)
            {
                SetNoStatus();
            }
        }

        public string GetMessageId()
        {
            return _messageId;
        }

        public void Create(string[] openIds, int time)
        {
            if (!_noStatus)
            {
                var now = (DateTime.UtcNow.Ticks - 621355968000000000) / 10000000;
                var span = time - int.Parse(now.ToString());
                var expire = new TimeSpan(6, 0, span);
                //总数
                _redis.StringSet(CacheKey.MessageStatus + _messageId + ":total", openIds.Count());
                //发送时间
                _redis.StringSet(CacheKey.MessageStatus + _messageId + ":time", time);
                //发送的所有用户
                foreach (var item in openIds)
                {
                    _redis.SetAdd(CacheKey.MessageStatus + _messageId + ":pending", item);
                }
                //创建清除计划任务
                var jobId = BackgroundJob.Schedule<MessageStatus>(x => x.CleanMessageInfo(_messageId, null), expire);
                _redis.StringSet(CacheKey.MessageCleanerIdPrefix + _messageId, jobId);
            }
        }

        public void Sended(string openId)
        {
            if (!_noStatus)
            {
                //将信息从pending标记为sended
                _redis.SetRemove(CacheKey.MessageStatus + _messageId + ":pending", openId);
                //已经发送的列表
                _redis.SetAdd(CacheKey.MessageStatus + _messageId + ":sended", openId);
            }
        }

        public void Success(string openId)
        {
            if (!_noStatus)
            {
                //将信息从正在发送中移除
                _redis.SetRemove(CacheKey.MessageStatus + _messageId + ":sended", openId);
                //发送成功的列表
                _redis.SetAdd(CacheKey.MessageStatus + _messageId + ":success", openId);
            }
        }

        public void UserBlock(string openId)
        {
            if (!_noStatus)
            {
                //将信息从正在发送中移除
                _redis.SetRemove(CacheKey.MessageStatus + _messageId + ":sended", openId); ;
                //用户拒收的列表
                _redis.SetAdd(CacheKey.MessageStatus + _messageId + ":user-block", openId);
            }
        }

        public void SystemFailed(string openId)
        {
            if (!_noStatus)
            {
                //将信息从正在发送中移除
                _redis.SetRemove(CacheKey.MessageStatus + _messageId + ":sended", openId);
                //系统错误发送失败的列表
                _redis.SetAdd(CacheKey.MessageStatus + _messageId + ":system-failed", openId);
            }
        }

        public void SendError(string openId)
        {
            if (!_noStatus)
            {
                //将信息从正在发送中移除
                _redis.SetRemove(CacheKey.MessageStatus + _messageId + ":sended", openId);
                //请求失败的列表
                _redis.SetAdd(CacheKey.MessageStatus + _messageId + ":send-error", openId);
            }
        }

        public void SetTemplateMap(string msgId)
        {
            if (!_noStatus)
            {
                //设置模版消息和消息ID对应
                _redis.StringSet(CacheKey.MessageStatus + "template-map:" + msgId, _messageId);
            }
        }

        public string GetTemplateMessageId(string msgId)
        {
            var tmpId = _redis.StringGet(CacheKey.MessageStatus + "template-map:" + msgId);
            _redis.KeyDelete(CacheKey.MessageStatus + "template-map:" + msgId);
            if (tmpId.IsNull)
            {
                return null;
            }
            else
            {
                if (_redis.StringGet(CacheKey.MessageStatus + tmpId + ":total").IsNull)
                {
                    return null;
                }
                else
                {
                    return tmpId.ToString();
                }
            }
        }

        [Queue("platform")]
        public void CleanMessageInfo(string messageId, PerformContext context = null)
        {
            context.WriteLine("开始清理消息记录：{0}", messageId);
            _redis.KeyDelete(CacheKey.MessageStatus + messageId + ":total");
            _redis.KeyDelete(CacheKey.MessageStatus + messageId + ":time");
            _redis.KeyDelete(CacheKey.MessageStatus + messageId + ":pending");
            _redis.KeyDelete(CacheKey.MessageStatus + messageId + ":sended");
            _redis.KeyDelete(CacheKey.MessageStatus + messageId + ":success");
            _redis.KeyDelete(CacheKey.MessageStatus + messageId + ":user-block");
            _redis.KeyDelete(CacheKey.MessageStatus + messageId + ":system-failed");
            _redis.KeyDelete(CacheKey.MessageStatus + messageId + ":send-error");
            _redis.KeyDelete(CacheKey.MessageJobIdMapPrefix + messageId);
            _redis.KeyDelete(CacheKey.MessageCleanerIdPrefix + messageId);
            context.WriteLine("清理消息记录：{0} 完毕", messageId);
        }

        public MessageStatusInfo GetInfo()
        {
            if (_redis.KeyExists(CacheKey.MessageStatus + _messageId + ":total"))
            {
                var info = new MessageStatusInfo
                {
                    Total = int.Parse(_redis.StringGet(CacheKey.MessageStatus + _messageId + ":total")),
                    Time = int.Parse(_redis.StringGet(CacheKey.MessageStatus + _messageId + ":time")),
                    PendingList = _redis.SetMembers(CacheKey.MessageStatus + _messageId + ":pending").ToStringArray(),
                    SendedList = _redis.SetMembers(CacheKey.MessageStatus + _messageId + ":sended").ToStringArray(),
                    SuccessList = _redis.SetMembers(CacheKey.MessageStatus + _messageId + ":success").ToStringArray(),
                    UserBlockList = _redis.SetMembers(CacheKey.MessageStatus + _messageId + ":user-block").ToStringArray(),
                    SystemFailedList = _redis.SetMembers(CacheKey.MessageStatus + _messageId + ":system-failed").ToStringArray(),
                    SendErrorList = _redis.SetMembers(CacheKey.MessageStatus + _messageId + ":send-error").ToStringArray(),
                };
                return info;
            }
            else
            {
                return null;
            }
        }

        public void AddJobId(string jobId)
        {
            if (!_noStatus)
            {
                _redis.SetAdd(CacheKey.MessageJobIdMapPrefix + _messageId, jobId);
            }
        }

        [Queue("platform")]
        public void CancelJob(string messageId, PerformContext context = null)
        {
            while (_redis.SetLength(CacheKey.MessageJobIdMapPrefix + messageId) > 0)
            {
                var tmpId = _redis.SetPop(CacheKey.MessageJobIdMapPrefix + messageId);
                BackgroundJob.Delete(tmpId);
                context.WriteLine("清除任务：" + tmpId);
            }
            var cleanJobId = _redis.StringGet(CacheKey.MessageCleanerIdPrefix + messageId);
            BackgroundJob.Requeue(cleanJobId);
            context.WriteLine("立即执行消息状态清理程序：" + cleanJobId);
        }
    }
}
