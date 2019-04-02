using System;
using StackExchange.Redis;
using Wechat.Web;
using Newtonsoft.Json;

namespace Wechat.Helpers
{

    /// <summary>
    /// 用户消息队列定义
    /// </summary>
    public class MessageQueue : RedisQueue<UserMessageRequsetXml>
    {
        public MessageQueue(IDatabase redis) : base(redis, CacheKey.MessageQueue) { }
    }

    /// <summary>
    /// 事件系统队列定义
    /// </summary>
    public class EventQueue : RedisQueue<UserMessageRequsetXml>
    {
        public EventQueue(IDatabase redis) : base(redis, CacheKey.EventQueue) { }
    }

    /// <summary>
    /// Redis队列实现
    /// </summary>
    public class RedisQueue<T>
    {
        IDatabase _redis { get; }
        string _queueName { get; }

        public long Count
        {
            get
            {
                return _redis.ListLength(_queueName);
            }
        }

        public RedisQueue(IDatabase redis, string queueName)
        {
            _queueName = queueName;
            _redis = redis;
        }

        public void Enqueue(T data)
        {
            _redis.ListLeftPush(_queueName, JsonConvert.SerializeObject(data));
        }

        public T Dequeue()
        {
            var data = _redis.ListRightPop(_queueName);
            if (data.IsNull)
            {
                return default(T);
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(data);
            }
        }
    }
}
