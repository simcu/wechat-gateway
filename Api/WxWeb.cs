using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Text;
using System.IO;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;

namespace Wechat.Api
{
    public static class WxWebApi
    {
        const string _getCodeUrl = "https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_base&state=&component_appid={2}#wechat_redirect";
        const string _getUserAccessToken = "https://api.weixin.qq.com/sns/oauth2/component/access_token?appid={0}&code={1}&grant_type=authorization_code&component_appid={2}&component_access_token={3}";
        const string _getUserInfo = "https://api.weixin.qq.com/cgi-bin/user/info?access_token={0}&openid={1}&lang=zh_CN";
        const string _getJsTicket = "https://api.weixin.qq.com/cgi-bin/ticket/getticket?type=jsapi&access_token=";
        const string _createQrCode = "https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=";
        const string _setMenu = "https://api.weixin.qq.com/cgi-bin/menu/create?access_token=";
        const string _deleteMenu = "https://api.weixin.qq.com/cgi-bin/menu/delete?access_token=";
        const string _getTemplateMessageList = "https://api.weixin.qq.com/cgi-bin/template/get_all_private_template?access_token=";
        const string _sendTemplateMessage = "https://api.weixin.qq.com/cgi-bin/message/template/send?access_token=";
        const string _uploadImage = "https://api.weixin.qq.com/cgi-bin/material/add_material?type=image&access_token=";
        const string _getImageList = "https://api.weixin.qq.com/cgi-bin/material/batchget_material?access_token=";
        const string _deleteImage = "https://api.weixin.qq.com/cgi-bin/material/del_material?access_token=";

        /// <summary>
        /// 获取用户授权CODE
        /// </summary>
        /// <returns>The code URL.</returns>
        /// <param name="componentAppId">第三方平台appid</param>
        /// <param name="appId">公众号的appid</param>
        /// <param name="redirectUrl">重定向地址，需要urlencode，这里填写的应是服务开发方的回调地址</param>
        public static string GetCodeUrl(string componentAppId, string appId, string redirectUrl)
        {
            return string.Format(_getCodeUrl, appId, redirectUrl, componentAppId);
        }

        /// <summary>
        /// 使用CODE获取用户AccessToken
        /// </summary>
        /// <returns>The access token.</returns>
        /// <param name="componentAppId">第三方平台appid</param>
        /// <param name="componentAccessToken">第三方平台component_access_token</param>
        /// <param name="appId">公众号的appid</param>
        /// <param name="code">微信前端跳转所获得的CODE</param>
        public static UserAccesstokenResponse GetAccessToken(string componentAppId, string componentAccessToken, string appId, string code)
        {
            var url = string.Format(_getUserAccessToken, appId, code, componentAppId, componentAccessToken);
            var resp = Http.GetQuery(url);
            return JsonConvert.DeserializeObject<UserAccesstokenResponse>(resp);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns>The user info.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="openId">用户openid</param>
        public static UserInfoResponse GetUserInfo(string accessToken, string openId)
        {
            var url = string.Format(_getUserInfo, accessToken, openId);
            var resp = Http.GetQuery(url);
            return JsonConvert.DeserializeObject<UserInfoResponse>(resp);
        }

        /// <summary>
        /// 获取JsTicket
        /// </summary>
        /// <returns>The js ticket.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        public static UserJsTicketResponse GetJsTicket(string accessToken)
        {
            var url = _getJsTicket + accessToken;
            var resp = Http.GetQuery(url);
            return JsonConvert.DeserializeObject<UserJsTicketResponse>(resp);
        }

        /// <summary>
        /// 获取带参数的临时二维码
        /// </summary>
        /// <returns>The qr code.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="data">场景值ID（字符串形式的ID），字符串类型，长度限制为1到64</param>
        public static CreateQrCodeResponse CreateQrCode(string accessToken, string data)
        {
            var url = _createQrCode + accessToken;
            var req = new CreateQrCodeRequest
            {
                ActionInfo = new CreateQrCodeRequest.ActionInfoItem
                {
                    Scene = new CreateQrCodeRequest.ActionInfoItem.SceneItem
                    {
                        SceneStr = data
                    }
                }
            };
            var resp = Http.PostQuery(url, req);
            return JsonConvert.DeserializeObject<CreateQrCodeResponse>(resp);
        }

        /// <summary>
        /// 设置菜单
        /// </summary>
        /// <returns>The menu.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="config">菜单配置的JSON字符串</param>
        public static BaseResponse SetMenu(string accessToken, string config)
        {
            var resp = Http.PostQueryRaw(_setMenu + accessToken, config);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }

        /// <summary>
        /// 删除菜单
        /// </summary>
        /// <returns>The menu.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        public static BaseResponse DeleteMenu(string accessToken)
        {
            var resp = Http.GetQuery(_deleteMenu + accessToken);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }

        /// <summary>
        /// 获取模版消息列表
        /// </summary>
        /// <returns>The template list.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        public static WxWebGetTemplateListResponse GetTemplateList(string accessToken)
        {
            var resp = Http.GetQuery(_getTemplateMessageList + accessToken);
            return JsonConvert.DeserializeObject<WxWebGetTemplateListResponse>(resp);
        }

        /// <summary>
        /// 发送模版消息
        /// </summary>
        /// <returns>The template.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="openId">接收者openid</param>
        /// <param name="templateId">模板ID</param>
        /// <param name="url">模板跳转链接（海外帐号没有跳转能力）</param>
        /// <param name="data">模板数据</param>
        public static WxWebSendTemplateResponse SendTemplate(string accessToken, string openId, string templateId, string url, Dictionary<string, WxWebSendTemplateRequest.DataItem> data)
        {
            var request = new WxWebSendTemplateRequest
            {
                ToUser = openId,
                TemplateId = templateId,
                Url = url,
                Data = data
            };
            var resp = Http.PostQuery(_sendTemplateMessage + accessToken, request);
            return JsonConvert.DeserializeObject<WxWebSendTemplateResponse>(resp);
        }

        /// <summary>
        /// 获取图片素材列表
        /// </summary>
        /// <returns>The image list.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="page">Page.</param>
        public static GetImageListResponse GetImageList(string accessToken, int page)
        {
            var url = _getImageList + accessToken;
            var req = new GetImageListRequest
            {
                Offset = (page - 1) * 20
            };
            var resp = Http.PostQuery(url, req);
            return JsonConvert.DeserializeObject<GetImageListResponse>(resp);
        }

        /// <summary>
        /// 删除图片
        /// </summary>
        /// <returns>The image.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="imageId">素材的media_id</param>
        public static BaseResponse DeleteImage(string accessToken, string imageId)
        {
            var url = _deleteImage + accessToken;
            var req = new DeleteImageRequest
            {
                MediaId = imageId
            };
            var resp = Http.PostQuery(url, req);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }

        /// <summary>
        /// 上传永久图片素材
        /// </summary>
        /// <returns>The image.</returns>
        /// <param name="accessToken">Access token.</param>
        /// <param name="file">要上传的文件的ByteArray</param>
        public static UploadImageApiResponse UploadImage(string accessToken, byte[] file)
        {
            var url = _uploadImage + accessToken;
            var http = new HttpClient();
            var boundary = "fbcfd42e-4e8e-4bf3-826d-cc3cf506fesd";
            var multipartForm = new MultipartFormDataContent(boundary);
            multipartForm.Headers.Remove("Content-Type");
            multipartForm.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
            var fileContent = new ByteArrayContent(file);
            fileContent.Headers.Remove("Content-Disposition");
            fileContent.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"media\";filename=\"111.png\"");
            multipartForm.Add(fileContent);
            var resp = http.PostAsync(url, multipartForm).Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<UploadImageApiResponse>(resp);
        }
    }
}
