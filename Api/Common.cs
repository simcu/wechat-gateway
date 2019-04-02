using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Wechat.Api
{
    public static class Http
    {
        /// <summary>
        /// 将request以json的形式发送到URL
        /// </summary>
        /// <returns>The query.</returns>
        /// <param name="url">URL.</param>
        /// <param name="request">请求类,会自动转换为JSON</param>
        public static string PostQuery(string url, object request)
        {
            return PostQueryRaw(url, JsonConvert.SerializeObject(request)); ;
        }

        /// <summary>
        /// 请求URL
        /// </summary>
        /// <returns>The query.</returns>
        /// <param name="url">URL.</param>
        public static string GetQuery(string url)
        {
            var webClient = new WebClient();
            byte[] resp = webClient.DownloadData(url);
            return Encoding.UTF8.GetString(resp);
        }

        /// <summary>
        /// POST提交json字符串
        /// </summary>
        /// <returns>The query raw.</returns>
        /// <param name="url">URL.</param>
        /// <param name="json">请求JSON字符串</param>
        public static string PostQueryRaw(string url, string json)
        {
            var webClient = new WebClient();
            webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] resp = webClient.UploadData(url, "POST", data);
            return Encoding.UTF8.GetString(resp);
        }

        public static string Upload(string url, byte[] data)
        {
            var webClient = new WebClient();
            //webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            byte[] resp = webClient.UploadData(url, "POST", data);
            return Encoding.UTF8.GetString(resp);
        }
    }

    /// <summary>
    /// 微信基础响应
    /// </summary>
    public class BaseResponse
    {
        [JsonProperty("errcode")]
        public int ErrCode { get; set; } = 0;
        [JsonProperty("errmsg")]
        public string ErrMsg { get; set; } = "ok";
    }

    public class UploadImageApiResponse : BaseResponse
    {
        [JsonProperty("media_id")]
        public string MediaId { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// 模版消息列表模型
    /// </summary>
    public class WxWebGetTemplateListResponse : BaseResponse
    {
        public class TemplateItem
        {
            [JsonProperty("template_id")]
            public string TemplateId { get; set; }
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("primary_industry")]
            public string PrimaryIndustry { get; set; }
            [JsonProperty("deputy_industry")]
            public string DeputyIndustry { get; set; }
            [JsonProperty("content")]
            public string Content { get; set; }
            [JsonProperty("example")]
            public string Example { get; set; }
        }
        [JsonProperty("template_list")]
        public TemplateItem[] TemplateList { get; set; }
    }

    /// <summary>
    /// 发送模版消息请求
    /// </summary>
    public class WxWebSendTemplateRequest
    {
        public class DataItem
        {
            [JsonProperty("value")]
            public string Value { get; set; }
            [JsonProperty("color")]
            public string Color { get; set; }
        }
        [JsonProperty("touser")]
        public string ToUser { get; set; }
        [JsonProperty("template_id")]
        public string TemplateId { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("data")]
        public Dictionary<string, DataItem> Data { get; set; }
    }

    /// <summary>
    /// 发送模版消息返回
    /// </summary>
    public class WxWebSendTemplateResponse : BaseResponse
    {
        [JsonProperty("msgid")]
        public long MsgId { get; set; }
    }

    /// <summary>
    /// 平台Access token请求模型
    /// </summary>
    public class ComponentAccessTokenRequest
    {
        [JsonProperty("component_appid")]
        public string ComponentAppId { get; set; }
        [JsonProperty("component_appsecret")]
        public string ComponentAppSecret { get; set; }
        [JsonProperty("component_verify_ticket")]
        public string ComponentVerifyTicket { get; set; }

    }

    /// <summary>
    /// 平台access token模型响应
    /// </summary>
    public class ComponentAccessTokenResponse : BaseResponse
    {
        [JsonProperty("component_access_token")]
        public string ComponentAccessToken { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class PreAuthCodeRequest
    {
        [JsonProperty("component_appid")]
        public string ComponentAppId { get; set; }
    }

    public class PreAuthCodeResponse : BaseResponse
    {
        [JsonProperty("pre_auth_code")]
        public string PreAuthCode { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class AuthInfoRequest
    {
        [JsonProperty("component_appid")]
        public string ComponentAppId { get; set; }
        [JsonProperty("authorization_code")]
        public string AuthorizationCode { get; set; }
    }

    public class AuthInfoResponse : BaseResponse
    {
        [JsonProperty("authorization_info")]
        public RefreshAccessTokenResponse AuthorizationInfo { get; set; }
    }

    public class RefreshAccessTokenRequest
    {
        [JsonProperty("component_appid")]
        public string ComponentAppId { get; set; }
        [JsonProperty("authorizer_appid")]
        public string AuthorizerAppId { get; set; }
        [JsonProperty("authorizer_refresh_token")]
        public string AuthorizerRefreshToken { get; set; }
    }

    public class RefreshAccessTokenResponse : BaseResponse
    {
        [JsonProperty("authorizer_appid")]
        public string AuthorizerAppId { get; set; }
        [JsonProperty("authorizer_access_token")]
        public string AuthorizerAccessToken { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonProperty("authorizer_refresh_token")]
        public string AuthorizerRefreshToken { get; set; }
    }

    public class AuthorizerInfoRequest
    {
        [JsonProperty("component_appid")]
        public string ComponentAppId { get; set; }
        [JsonProperty("authorizer_appid")]
        public string AuthorizerAppId { get; set; }
    }
    public class AuthorizerInfoResponse : BaseResponse
    {
        [JsonProperty("authorizer_info")]
        public AuthorizerInfo AuthorizerInfo { get; set; }
        [JsonProperty("authorization_info")]
        public AuthorizationInfo AuthorizationInfo { get; set; }

    }

    public class AuthorizationInfo
    {
        [JsonProperty("authorizer_appid")]
        public string AuthorizerAppId { get; set; }
        [JsonProperty("authorizer_refresh_token")]
        public string AuthorizerRefreshToken { get; set; }
        [JsonProperty("func_info")]
        public FuncInfo[] FuncInfos { get; set; }
        public class FuncInfo
        {
            [JsonProperty("funcscope_category")]
            public IdItem Category { get; set; }
        }
    }

    public class AuthorizerInfo
    {
        public class MiniProgramInfoItem
        {
            public class NetworkItem
            {
                [JsonProperty("RequestDomain")]
                public string[] RequestDomain { get; set; }
                [JsonProperty("WsRequestDomain")]
                public string[] WsRequestDomain { get; set; }
                [JsonProperty("UploadDomain")]
                public string[] UploadDomain { get; set; }
                [JsonProperty("DownloadDomain")]
                public string[] DownloadDomain { get; set; }
            }
            public class CategoryItem
            {
                [JsonProperty("first")]
                public string First { get; set; }
                [JsonProperty("second")]
                public string Second { get; set; }
            }
            [JsonProperty("network")]
            public NetworkItem Network { get; set; }
            [JsonProperty("categories")]
            public CategoryItem[] Categories { get; set; }
            [JsonProperty("visit_status")]
            public int VisitStatus { get; set; }
        }


        [JsonProperty("nick_name")]
        public string NickName { get; set; } = string.Empty;
        [JsonProperty("head_img")]
        public string HeadImg { get; set; } = string.Empty;
        [JsonProperty("user_name")]
        public string UserName { get; set; } = string.Empty;
        [JsonProperty("principal_name")]
        public string PrincipalName { get; set; } = string.Empty;
        [JsonProperty("service_type_info")]
        public IdItem ServiceTypeInfo { get; set; }
        [JsonProperty("verify_type_info")]
        public IdItem VerifyTypeInfo { get; set; }
        [JsonProperty("alias")]
        public string Alias { get; set; } = string.Empty;
        [JsonProperty("qrcode_url")]
        public string QrcodeUrl { get; set; } = string.Empty;
        [JsonProperty("miniprograminfo")]
        public MiniProgramInfoItem MiniProgramInfo { get; set; } = null;

    }

    public class IdItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class UserAccessTokenRequest
    {
        public string UserAppId { get; set; }
        public string Code { get; set; }
        public string ComponentAppId { get; set; }
        public string ComponentAccessToken { get; set; }
    }
    public class UserAccesstokenResponse : BaseResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        [JsonProperty("openid")]
        public string OpenId { get; set; }
        [JsonProperty("scope")]
        public string Scope { get; set; }
    }

    public class UserJsTicketResponse : BaseResponse
    {
        [JsonProperty("ticket")]
        public string Ticket { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class UserSessionKeyResponse : BaseResponse
    {
        [JsonProperty("openid")]
        public string OpenId { get; set; }
        [JsonProperty("session_key")]
        public string SessionKey { get; set; }
        [JsonProperty("unionid")]
        public string UnionId { get; set; }

    }

    public class CreateQrCodeRequest
    {
        public class ActionInfoItem
        {
            public class SceneItem
            {
                [JsonProperty("scene_str")]
                public string SceneStr { get; set; }
            }
            [JsonProperty("scene")]
            public SceneItem Scene { get; set; }
        }
        [JsonProperty("action_name")]
        public string ActionName { get; set; } = "QR_STR_SCENE";
        [JsonProperty("action_info")]
        public ActionInfoItem ActionInfo { get; set; }
        [JsonProperty("expire_seconds")]
        public int ExpireSeconds { get; set; } = 2592000;
    }

    public class CreateQrCodeResponse : BaseResponse
    {
        [JsonProperty("ticket")]
        public string Ticket { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("expire_seconds")]
        public int ExpireSeconds { get; set; }
    }

    public class UserInfoResponse : BaseResponse
    {
        [JsonProperty("subscribe")]
        public int Subscribe { get; set; }
        [JsonProperty("openid")]
        public string OpenId { get; set; } = string.Empty;
        [JsonProperty("nickname")]
        public string NickName { get; set; } = string.Empty;
        [JsonProperty("sex")]
        public int Sex { get; set; }
        [JsonProperty("city")]
        public string City { get; set; } = string.Empty;
        [JsonProperty("province")]
        public string Province { get; set; } = string.Empty;
        [JsonProperty("country")]
        public string Country { get; set; } = string.Empty;
        [JsonProperty("headimgurl")]
        public string HeadImgUrl { get; set; } = string.Empty;
        [JsonProperty("subscribe_time")]
        public int SubscribeTime { get; set; }
        [JsonProperty("unionid")]
        public string UnionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 客服消息-文字
    /// </summary>
    public class TextMessage
    {
        public class TextContent
        {
            [JsonProperty("content")]
            public string Content { get; set; }
        }
        [JsonProperty("touser")]
        public string ToUser { get; set; }
        [JsonProperty("msgtype")]
        public string MsgType { get; set; } = "text";
        [JsonProperty("text")]
        public TextContent Text { get; set; }
    }

    /// <summary>
    /// 客服消息-图片
    /// </summary>
    public class ImageMessage
    {
        public class ImageContent
        {
            [JsonProperty("media_id")]
            public string MediaId { get; set; }
        }
        [JsonProperty("touser")]
        public string ToUser { get; set; }
        [JsonProperty("msgtype")]
        public string MsgType { get; set; } = "image";
        [JsonProperty("image")]
        public ImageContent Image { get; set; }
    }

    /// <summary>
    /// 客服消息-图文
    /// </summary>
    public class NewsMessage
    {
        public class NewsContent
        {
            public class Article
            {
                [JsonProperty("title")]
                public string Title { get; set; }
                [JsonProperty("description")]
                public string Description { get; set; }
                [JsonProperty("url")]
                public string Url { get; set; }
                [JsonProperty("picurl")]
                public string PicUrl { get; set; }
            }
            [JsonProperty("articles")]
            public Article[] Articles { get; set; }
        }
        [JsonProperty("touser")]
        public string ToUser { get; set; }
        [JsonProperty("msgtype")]
        public string MsgType { get; set; } = "news";
        [JsonProperty("news")]
        public NewsContent News { get; set; }

        public NewsMessage(string openId, string title, string description, string url, string picUrl)
        {
            ToUser = openId;
            var article = new NewsContent.Article
            {
                Title = title,
                Description = description,
                Url = url,
                PicUrl = picUrl
            };
            News = new NewsContent
            {
                Articles = new NewsContent.Article[] { article }
            };
        }
    }

    public class LinkMessage
    {
        public class LinkContent
        {
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("thumb_url")]
            public string PicUrl { get; set; }
            [JsonProperty("url")]
            public string Url { get; set; }
        }
        [JsonProperty("touser")]
        public string ToUser { get; set; }
        [JsonProperty("msgtype")]
        public string MsgType { get; set; } = "link";
        [JsonProperty("link")]
        public LinkContent Link { get; set; }

        public LinkMessage(string openId, string title, string description, string url, string picUrl)
        {
            ToUser = openId;
            Link = new LinkContent
            {
                Title = title,
                Description = description,
                Url = url,
                PicUrl = picUrl
            };
        }
    }

    public class WxAppMessage
    {
        public class WxAppContent
        {
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("appid")]
            public string AppId { get; set; }
            [JsonProperty("pagepath")]
            public string PagePath { get; set; }
            [JsonProperty("thumb_media_id")]
            public string MediaId { get; set; }
        }
        [JsonProperty("touser")]
        public string ToUser { get; set; }
        [JsonProperty("msgtype")]
        public string MsgType { get; set; } = "miniprogrampage";
        [JsonProperty("miniprogrampage")]
        public WxAppContent Content { get; set; }

        public WxAppMessage(string openId, string title, string appId, string pagePath, string mediaId)
        {
            ToUser = openId;
            Content = new WxAppContent
            {
                Title = title,
                AppId = appId,
                PagePath = pagePath,
                MediaId = mediaId
            };
        }
    }

    public class GetImageListRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "image";
        [JsonProperty("offset")]
        public int Offset { get; set; }
        [JsonProperty("count")]
        public int Count { get; set; } = 20;
    }

    public class GetImageListResponse : BaseResponse
    {
        public class ListItem
        {
            [JsonProperty("media_id")]
            public string MediaId { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("update_time")]
            public int UpdateTime { get; set; }
            [JsonProperty("url")]
            public string Url { get; set; }
        }
        [JsonProperty("total_count")]
        public int TotalCount { get; set; }
        [JsonProperty("item_count")]
        public int ListCount { get; set; }
        [JsonProperty("item")]
        public ListItem[] List { get; set; }
    }

    public class DeleteImageRequest
    {
        [JsonProperty("media_id")]
        public string MediaId { get; set; }
    }

    public class ModifyDomainRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; }
        [JsonProperty("requestdomain")]
        public string[] RequestDomain { get; set; }
        [JsonProperty("wsrequestdomain")]
        public string[] WsRequestDomain { get; set; }
        [JsonProperty("uploaddomain")]
        public string[] UploadDomain { get; set; }
        [JsonProperty("downloaddomain")]
        public string[] DownloadDomain { get; set; }

    }

    public class WxAppCommitRequest
    {
        [JsonProperty("template_id")]
        public int TemplateId { get; set; }
        [JsonProperty("ext_json")]
        public string ExtJson { get; set; }
        [JsonProperty("user_version")]
        public string UserVersion { get; set; }
        [JsonProperty("user_desc")]
        public string UserDesc { get; set; }
    }

    public class WxAppSubmitAuditRequest
    {
        public class Item
        {
            [JsonProperty("address")]
            public string Address { get; set; }
            [JsonProperty("tag")]
            public string Tag { get; set; }
            [JsonProperty("first_class")]
            public string FirstClass { get; set; }
            [JsonProperty("second_class")]
            public string SecondClass { get; set; }
            [JsonProperty("first_id")]
            public int FirstId { get; set; }
            [JsonProperty("second_id")]
            public int SecondId { get; set; }
            [JsonProperty("title")]
            public string Title { get; set; }
        }
        [JsonProperty("item_list")]
        public Item[] ItemList { get; set; }
    }

    public class WxAppSubmitAuditResponse : BaseResponse
    {
        [JsonProperty("auditid")]
        public long AuditId { get; set; }
    }

    public class WxAppGetAuditStatusRequest
    {
        [JsonProperty("auditid")]
        public long AuditId { get; set; }
    }

    public class WxAppGetAuditStatusResponse : BaseResponse
    {
        [JsonProperty("status")]
        public int Status { get; set; }
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class WxAppChangeVisitStatusRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; }
    }

    public class WxAppGetTemplateListRequest
    {
        [JsonProperty("offset")]
        public int Offset { get; set; }
        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public class WxAppGetTemplateListResponse : BaseResponse
    {
        public class ListItem
        {
            [JsonProperty("template_id")]
            public string TemplateId { get; set; }
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("content")]
            public string Content { get; set; }
            [JsonProperty("example")]
            public string Example { get; set; }
        }
        [JsonProperty("list")]
        public ListItem[] TemplateList { get; set; }
    }

    public class WxAppSendTemplateRequest
    {
        public class DataItem
        {
            [JsonProperty("value")]
            public string Value { get; set; }
        }
        [JsonProperty("touser")]
        public string ToUser { get; set; }
        [JsonProperty("template_id")]
        public string TemplateId { get; set; }
        [JsonProperty("page")]
        public string Page { get; set; }
        [JsonProperty("data")]
        public Dictionary<string, DataItem> Data { get; set; }
        [JsonProperty("form_id")]
        public string FormId { get; set; }
    }
}
