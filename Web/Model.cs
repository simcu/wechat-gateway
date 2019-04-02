using System;
using System.Xml;
using Tencent;
using System.Xml.Serialization;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Wechat.Web
{
    /// <summary>
    /// 消息推送QueryString模型
    /// </summary>
    public class MessageRequestQuery
    {
        public string Signature { get; set; }
        public string EchoStr { get; set; }
        public string Timestamp { get; set; }
        public string Nonce { get; set; }
        public string Msg_Signature { get; set; }
    }

    [XmlRoot("xml")]
    public class MessageRequsetBody
    {
        public string ToUserName { get; set; }
        public string Encrypt { get; set; }
    }

    /// <summary>
    /// 微信推送基础模型
    /// </summary>
    public class BaseRequestXml
    {
        public XmlNode Origin { get; set; }

        public BaseRequestXml(string content)
        {
            var doc = new XmlDocument() { XmlResolver = null };
            doc.LoadXml(content);
            Origin = doc.FirstChild;
            ProcessData();
        }
        public BaseRequestXml() { }

        /// <summary>
        /// 获取XML中的内容
        /// </summary>
        /// <returns>The item.</returns>
        /// <param name="name">Name.</param>
        public string GetItem(string name)
        {
            return Origin[name]?.InnerText;
        }

        protected virtual void ProcessData() { }
    }

    /// <summary>
    /// 微信推送系统信息
    /// </summary>
    public class PlatformMessageRequestXml : BaseRequestXml
    {
        public string AppId { get; set; }
        public int CreateTime { get; set; }
        public string InfoType { get; set; }
        public string ComponentVerifyTicket { get; set; }
        public string AuthorizerAppId { get; set; }
        public string AuthorizationCode { get; set; }
        public string AuthorizationCodeExpiredTime { get; set; }
        public string PreAuthCode { get; set; }

        public PlatformMessageRequestXml(string content) : base(content) { }
        protected override void ProcessData()
        {
            AppId = GetItem("AppId");
            CreateTime = int.Parse(GetItem("CreateTime"));
            InfoType = GetItem("InfoType");
            ComponentVerifyTicket = GetItem("ComponentVerifyTicket");
            AuthorizerAppId = GetItem("AuthorizerAppid");
            AuthorizationCode = GetItem("AuthorizationCode");
            AuthorizationCodeExpiredTime = GetItem("AuthorizationCodeExpiredTime");
            PreAuthCode = GetItem("PreAuthCode");
        }
    }

    /// <summary>
    /// 消息推送XML模型
    /// </summary>
    public class UserMessageRequsetXml : BaseRequestXml
    {
        public string MsgId { get; set; }
        public string MsgType { get; set; }
        public int CreateTime { get; set; }
        public string FromUserName { get; set; }
        public string ToUserName { get; set; }
        public string Content { get; set; }
        public string Event { get; set; }
        public string EventKey { get; set; }
        public string PicUrl { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public double Location_X { get; set; }
        public double Location_Y { get; set; }
        public string Lable { get; set; }
        public int Scale { get; set; }
        public string MediaId { get; set; }
        public string Format { get; set; }
        public string Recognition { get; set; }
        public string ThumbMediaId { get; set; }
        public string AppId { get; set; }
        public int SuccTime { get; set; }
        public string Reason { get; set; }
        public int FailTime { get; set; }
        public string Status { get; set; }

        public UserMessageRequsetXml() { }
        public UserMessageRequsetXml(string content) : base(content) { }

        protected override void ProcessData()
        {
            MsgId = GetItem("MsgId") ?? GetItem("MsgID");
            MsgType = GetItem("MsgType");
            CreateTime = GetItem("CreateTime") != null ? int.Parse(GetItem("CreateTime")) : 0;
            FromUserName = GetItem("FromUserName");
            ToUserName = GetItem("ToUserName");
            Content = GetItem("Content");
            Event = GetItem("Event");
            EventKey = GetItem("EventKey");
            PicUrl = GetItem("PicUrl");
            Url = GetItem("Url");
            Title = GetItem("Title");
            Description = GetItem("Description");
            Location_X = GetItem("Location_X") != null ? double.Parse(GetItem("Location_X")) : 0;
            Location_Y = GetItem("Location_Y") != null ? double.Parse(GetItem("Location_Y")) : 0;
            Lable = GetItem("Lable");
            Scale = GetItem("Scale") != null ? int.Parse(GetItem("Scale")) : 0;
            MediaId = GetItem("MediaId");
            Format = GetItem("Format");
            Recognition = GetItem("Recognition");
            ThumbMediaId = GetItem("ThumbMediaId");
            AppId = GetItem("AppId");
            SuccTime = GetItem("SuccTime") != null ? int.Parse(GetItem("SuccTime")) : 0;
            Reason = GetItem("Status");
            FailTime = GetItem("FailTime") != null ? int.Parse(GetItem("FailTime")) : 0;
            Status = GetItem("Status");

        }
    }

    public static class TemplateMessageStatus
    {
        public const string Pending = "pending";
        public const string Sended = "sended";
        public const string Success = "success";
        public const string UserBlock = "failed:user block";
        public const string SystemFailed = "failed: system failed";
    }
}
