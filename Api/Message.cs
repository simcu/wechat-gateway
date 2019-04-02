using System;
using Newtonsoft.Json;
using System.Collections.Generic;
namespace Wechat.Api
{
    public static class MessageApi
    {
        const string _customMessage = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=";

        /// <summary>
        /// 给用户发送文本信息
        /// </summary>
        /// <returns>The text.</returns>
        /// <param name="accessToken">Access token.</param>
        /// <param name="openId">Open identifier.</param>
        /// <param name="content">Content.</param>
        public static BaseResponse SendText(string accessToken, string openId, string content)
        {
            var msg = new TextMessage
            {
                ToUser = openId,
                Text = new TextMessage.TextContent
                {
                    Content = content
                }
            };
            var resp = Http.PostQuery(_customMessage + accessToken, msg);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }

        /// <summary>
        /// 给用户发送图文信息(小程序为Link信息)
        /// </summary>
        /// <returns>The news.</returns>
        /// <param name="accessToken">接口调用凭证</param>
        /// <param name="openId">用户的 OpenID</param>
        /// <param name="title">消息标题</param>
        /// <param name="description">图文链接消息描述</param>
        /// <param name="url">图文链接消息被点击后跳转的链接</param>
        /// <param name="picUrl">图文链接消息的图片链接，支持 JPG、PNG 格式，较好的效果为大图 640 X 320，小图 80 X 80</param>
        /// <param name="wxApp">是否为小程序,小程序将发送Link消息</param>
        public static BaseResponse SendNews(string accessToken, string openId, string title, string description, string url, string picUrl, bool wxApp = false)
        {
            string resp;
            if (wxApp)
            {
                var msg = new NewsMessage(openId, title, description, url, picUrl);
                resp = Http.PostQuery(_customMessage + accessToken, msg);
            }
            else
            {
                var msg = new LinkMessage(openId, title, description, url, picUrl);
                resp = Http.PostQuery(_customMessage + accessToken, msg);
            }
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }

        /// <summary>
        /// 发送图片消息
        /// </summary>
        /// <returns>The image.</returns>
        /// <param name="accessToken">Access token.</param>
        /// <param name="openId">Open identifier.</param>
        /// <param name="mediaId">Media identifier.</param>
        public static BaseResponse SendImage(string accessToken, string openId, string mediaId)
        {
            var msg = new ImageMessage
            {
                ToUser = openId,
                Image = new ImageMessage.ImageContent
                {
                    MediaId = mediaId
                }
            };
            var resp = Http.PostQuery(_customMessage + accessToken, msg);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }



        /// <summary>
        /// 发送小程序卡片（公众号:要求小程序与公众号已关联）
        /// </summary>
        /// <returns>The wx app card.</returns>
        /// <param name="accessToken">Access token.</param>
        /// <param name="openId">Open identifier.</param>
        /// <param name="title">Title.</param>
        /// <param name="appId">App identifier.</param>
        /// <param name="pagePath">Page path.</param>
        /// <param name="imageId">Image identifier.</param>
        public static BaseResponse SendWxAppCard(string accessToken, string openId, string title, string appId, string pagePath, string imageId)
        {
            var msg = new WxAppMessage(openId, title, appId, pagePath, imageId);
            var resp = Http.PostQuery(_customMessage + accessToken, msg);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }


    }
}
