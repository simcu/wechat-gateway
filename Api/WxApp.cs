using Newtonsoft.Json;
using System.Collections.Generic;

namespace Wechat.Api
{
    public static class WxAppApi
    {
        const string _getSessionKey = "https://api.weixin.qq.com/sns/component/jscode2session?appid={0}&js_code={1}&grant_type=authorization_code&component_appid={2}&component_access_token={3}";
        const string _modifyDomain = "https://api.weixin.qq.com/wxa/modify_domain?access_token=";
        const string _wxaCommit = "https://api.weixin.qq.com/wxa/commit?access_token=";
        const string _wxaSubmitAudit = "https://api.weixin.qq.com/wxa/submit_audit?access_token=";
        const string _wxaGetAuditStatus = "https://api.weixin.qq.com/wxa/get_auditstatus?access_token=";
        const string _wxaRelease = "https://api.weixin.qq.com/wxa/release?access_token=";
        const string _wxaChangeVisitStatus = "https://api.weixin.qq.com/wxa/change_visitstatus?access_token=";
        const string _getTemplateList = "https://api.weixin.qq.com/cgi-bin/wxopen/template/list?access_token=";
        const string _sendTemplateMessage = "https://api.weixin.qq.com/cgi-bin/message/wxopen/template/send?access_token=";

        /// <summary>
        /// 修改小程序服务器域名
        /// </summary>
        /// <returns>The domain.</returns>
        /// <param name="componentAccessToken">请使用第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="requestDomain">request合法域名，当action参数是get时不需要此字段</param>
        /// <param name="wsRequestDomain">socket合法域名，当action参数是get时不需要此字段</param>
        /// <param name="uploadDomian">uploadFile合法域名，当action参数是get时不需要此字段</param>
        /// <param name="downloadDomain">downloadFile合法域名，当action参数是get时不需要此字段</param>
        /// <param name="action">add添加, delete删除, set覆盖, get获取。当参数是get时不需要填四个域名字段</param>
        public static BaseResponse ModifyDomain(string componentAccessToken, string[] requestDomain, string[] wsRequestDomain, string[] uploadDomian, string[] downloadDomain, string action = "set")
        {
            var request = new ModifyDomainRequest
            {
                Action = action,
                RequestDomain = requestDomain,
                WsRequestDomain = wsRequestDomain,
                UploadDomain = uploadDomian,
                DownloadDomain = downloadDomain
            };
            var resp = Http.PostQuery(_modifyDomain + componentAccessToken, request);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }

        /// <summary>
        /// 为授权的小程序帐号上传小程序代码
        /// </summary>
        /// <returns>The commit.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="templateId">代码库中的代码模版ID</param>
        /// <param name="extJson">第三方自定义的配置JSON字符串</param>
        /// <param name="userVersion">代码版本号，开发者可自定义</param>
        /// <param name="userDesc">代码描述，开发者可自定义</param>
        public static BaseResponse Commit(string accessToken, int templateId, string extJson, string userVersion, string userDesc)
        {
            var request = new WxAppCommitRequest
            {
                TemplateId = templateId,
                ExtJson = extJson,
                UserVersion = userVersion,
                UserDesc = userDesc
            };
            var resp = Http.PostQuery(_wxaCommit + accessToken, request);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }

        /// <summary>
        /// 将第三方提交的代码包提交审核（仅供第三方开发者代小程序调用）
        /// </summary>
        /// <returns>The audit.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="address">小程序的页面地址</param>
        /// <param name="tag">小程序的标签，多个标签用空格分隔，标签不能多于10个，标签长度不超过20</param>
        /// <param name="firstClass">一级类目名称</param>
        /// <param name="secondClass">二级类目名称</param>
        /// <param name="firstId">一级类目的ID</param>
        /// <param name="secondId">二级类目的ID</param>
        /// <param name="title">小程序页面的标题,标题长度不超过32</param>
        public static WxAppSubmitAuditResponse SubmitAudit(string accessToken, string address, string tag, string firstClass, string secondClass, int firstId, int secondId, string title)
        {
            var request = new WxAppSubmitAuditRequest
            {
                ItemList = new WxAppSubmitAuditRequest.Item[] {
                    new WxAppSubmitAuditRequest.Item
                    {
                        Address = address,
                        Tag = tag,
                        FirstClass = firstClass,
                        SecondClass = secondClass,
                        FirstId = firstId,
                        SecondId = secondId,
                        Title = title
                    }
                }
            };
            return SumitAuditDual(accessToken, request);
        }

        /// <summary>
        /// [多个页面]将第三方提交的代码包提交审核（仅供第三方开发者代小程序调用）
        /// </summary>
        /// <returns>The audit dual.</returns>
        /// <param name="accessToken">Access token.</param>
        /// <param name="request">Request.</param>
        public static WxAppSubmitAuditResponse SumitAuditDual(string accessToken, WxAppSubmitAuditRequest request)
        {
            var resp = Http.PostQuery(_wxaSubmitAudit + accessToken, request);
            return JsonConvert.DeserializeObject<WxAppSubmitAuditResponse>(resp);
        }

        /// <summary>
        /// 查询某个指定版本的审核状态
        /// </summary>
        /// <returns>The audit status.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="auditId">提交审核时获得的审核id</param>
        public static WxAppGetAuditStatusResponse GetAuditStatus(string accessToken, long auditId)
        {
            var request = new WxAppGetAuditStatusRequest
            {
                AuditId = auditId
            };
            var resp = Http.PostQuery(_wxaGetAuditStatus + accessToken, request);
            return JsonConvert.DeserializeObject<WxAppGetAuditStatusResponse>(resp);
        }

        /// <summary>
        /// 发布已通过审核的小程序
        /// </summary>
        /// <returns>The release.</returns>
        /// <param name="accesssToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        public static BaseResponse Release(string accesssToken)
        {
            var resp = Http.PostQueryRaw(_wxaRelease + accesssToken, "{}");
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }

        /// <summary>
        /// 修改小程序线上代码的可见状态
        /// </summary>
        /// <returns>The visit status.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="show">设置可访问状态，发布后默认可访问，false为不可见，true为可见</param>
        public static BaseResponse ChangeVisitStatus(string accessToken, bool show)
        {
            var request = new WxAppChangeVisitStatusRequest
            {
                Action = show ? "open" : "close"
            };
            var resp = Http.PostQuery(_wxaChangeVisitStatus + accessToken, request);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }

        /// <summary>
        /// 小程序CODE获取SessionKey
        /// </summary>
        /// <returns>The session key.</returns>
        /// <param name="componentAppId">Component app identifier.</param>
        /// <param name="componentAccessToken">Component access token.</param>
        /// <param name="appId">App identifier.</param>
        /// <param name="code">Code.</param>
        public static UserSessionKeyResponse GetSessionKey(string componentAppId, string componentAccessToken, string appId, string code)
        {
            var url = string.Format(_getSessionKey, appId, code, componentAppId, componentAccessToken);
            var resp = Http.GetQuery(url);
            return JsonConvert.DeserializeObject<UserSessionKeyResponse>(resp);
        }

        /// <summary>
        /// 获取帐号下已存在的模板列表
        /// </summary>
        /// <returns>The template.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="offset">用于分页，表示从offset开始。从 0 开始计数。</param>
        /// <param name="count">用于分页，表示拉取count条记录。最大为 20。最后一页的list长度可能小于请求的count。</param>
        public static WxAppGetTemplateListResponse GetTemplateList(string accessToken, int offset = 0, int count = 20)
        {
            var request = new WxAppGetTemplateListRequest
            {
                Offset = offset,
                Count = count
            };
            var resp = Http.PostQuery(_getTemplateList + accessToken, request);
            return JsonConvert.DeserializeObject<WxAppGetTemplateListResponse>(resp);
        }

        /// <summary>
        /// 发送模板消息
        /// </summary>
        /// <returns>The template.</returns>
        /// <param name="accessToken">第三方平台获取到的该小程序授权的authorizer_access_token</param>
        /// <param name="openId">接收者（用户）的 openid</param>
        /// <param name="templateId">所需下发的模板消息的id</param>
        /// <param name="page">点击模板卡片后的跳转页面，仅限本小程序内的页面。支持带参数,（示例index?foo=bar）。该字段不填则模板无跳转。</param>
        /// <param name="formId">表单提交场景下，为 submit 事件带上的 formId；支付场景下，为本次支付的 prepay_id</param>
        /// <param name="data">模板内容，不填则下发空模板</param>
        public static BaseResponse SendTemplate(string accessToken, string openId, string templateId, string page, string formId, Dictionary<string, WxAppSendTemplateRequest.DataItem> data)
        {
            var request = new WxAppSendTemplateRequest
            {
                ToUser = openId,
                TemplateId = templateId,
                Page = page,
                FormId = formId,
                Data = data
            };
            var resp = Http.PostQuery(_sendTemplateMessage + accessToken, request);
            return JsonConvert.DeserializeObject<BaseResponse>(resp);
        }
    }
}
