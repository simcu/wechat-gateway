using Newtonsoft.Json;

namespace Wechat.Api
{
    public static class ComponentApi
    {
        const string _getComponentAccessToken = "https://api.weixin.qq.com/cgi-bin/component/api_component_token";
        const string _getPreAuthCode = "https://api.weixin.qq.com/cgi-bin/component/api_create_preauthcode?component_access_token=";
        const string _getAuth = "https://api.weixin.qq.com/cgi-bin/component/api_query_auth?component_access_token=";
        const string _getAuthorizerAccessToken = "https://api.weixin.qq.com/cgi-bin/component/api_authorizer_token?component_access_token=";
        const string _getAuthorizerInfo = "https://api.weixin.qq.com/cgi-bin/component/api_get_authorizer_info?component_access_token=";
        const string _getBindUrlQrcode = "https://mp.weixin.qq.com/cgi-bin/componentloginpage?auth_type={0}&component_appid={1}&pre_auth_code={2}&redirect_uri={3}";
        const string _getBindUrlLink = "https://mp.weixin.qq.com/safe/bindcomponent?action=bindcomponent&auth_type={0}&no_scan=1&component_appid={1}&pre_auth_code={2}&redirect_uri={3}#wechat_redirect";

        /// <summary>
        /// 获取第三方平台component_access_token
        /// </summary>
        /// <returns>The component access token.</returns>
        /// <param name="componentAppId">第三方平台appid</param>
        /// <param name="componentAppSecret">第三方平台appsecret</param>
        /// <param name="componentVerifyTicket">微信后台推送的ticket，此ticket会定时推送</param>
        public static ComponentAccessTokenResponse GetComponentAccessToken(string componentAppId, string componentAppSecret, string componentVerifyTicket)
        {
            var request = new ComponentAccessTokenRequest
            {
                ComponentAppId = componentAppId,
                ComponentAppSecret = componentAppSecret,
                ComponentVerifyTicket = componentVerifyTicket
            };
            var resp = Http.PostQuery(_getComponentAccessToken, request);
            return JsonConvert.DeserializeObject<ComponentAccessTokenResponse>(resp);

        }

        /// <summary>
        /// 获取预授权码pre_auth_code
        /// </summary>
        /// <returns>The pre auth code.</returns>
        /// <param name="componentAccessToken">第三方平台component_access_token</param>
        /// <param name="componentAppId">第三方平台方appid</param>
        public static PreAuthCodeResponse GetPreAuthCode(string componentAccessToken, string componentAppId)
        {
            var request = new PreAuthCodeRequest
            {
                ComponentAppId = componentAppId
            };
            var resp = Http.PostQuery(_getPreAuthCode + componentAccessToken, request);
            return JsonConvert.DeserializeObject<PreAuthCodeResponse>(resp);
        }

        /// <summary>
        /// 使用授权码换取公众号或小程序的接口调用凭据和授权信息
        /// </summary>
        /// <returns>The auth info.</returns>
        /// <param name="componentAccessToken">第三方平台component_access_token</param>
        /// <param name="componentAppId">第三方平台appid</param>
        /// <param name="authorizationCode">授权code,会在授权成功时返回给第三方平台</param>
        public static AuthInfoResponse GetAuthInfo(string componentAccessToken, string componentAppId, string authorizationCode)
        {
            var request = new AuthInfoRequest
            {
                ComponentAppId = componentAppId,
                AuthorizationCode = authorizationCode
            };
            var resp = Http.PostQuery(_getAuth + componentAccessToken, request);
            return JsonConvert.DeserializeObject<AuthInfoResponse>(resp);
        }

        /// <summary>
        /// 获取（刷新）授权公众号或小程序的接口调用凭据（令牌）
        /// </summary>
        /// <returns>The access token.</returns>
        /// <param name="componentAccessToken">第三方平台component_access_token</param>
        /// <param name="componentAppId">第三方平台appid</param>
        /// <param name="appId">授权方appid</param>
        /// <param name="refreshToken">授权方的刷新令牌，刷新令牌主要用于第三方平台获取和刷新已授权用户的access_token</param>
        public static RefreshAccessTokenResponse RefreshAccessToken(string componentAccessToken, string componentAppId, string appId, string refreshToken)
        {
            var request = new RefreshAccessTokenRequest
            {
                ComponentAppId = componentAppId,
                AuthorizerAppId = appId,
                AuthorizerRefreshToken = refreshToken
            };
            var resp = Http.PostQuery(_getAuthorizerAccessToken + componentAccessToken, request);
            return JsonConvert.DeserializeObject<RefreshAccessTokenResponse>(resp);
        }

        /// <summary>
        /// 获取授权方的帐号基本信息
        /// </summary>
        /// <returns>The authorizer info.</returns>
        /// <param name="componentAccessToken">第三方平台component_access_token.</param>
        /// <param name="componentAppId">第三方平台appid</param>
        /// <param name="appId">授权方appid</param>
        public static AuthorizerInfoResponse GetAuthorizerInfo(string componentAccessToken, string componentAppId, string appId)
        {
            var request = new AuthorizerInfoRequest
            {
                ComponentAppId = componentAppId,
                AuthorizerAppId = appId
            };
            var resp = Http.PostQuery(_getAuthorizerInfo + componentAccessToken, request);
            return JsonConvert.DeserializeObject<AuthorizerInfoResponse>(resp);
        }

        /// <summary>
        /// 授权注册页面授权
        /// </summary>
        /// <returns>The bind URL.</returns>
        /// <param name="componentAppId">第三方平台方appid</param>
        /// <param name="preAuthCode">预授权码</param>
        /// <param name="redirectUri">回调URI</param>
        /// <param name="useMobile">true为点击移动端链接快速授权，false为授权注册页面扫码授权</param>
        /// <param name="authType">要授权的帐号类型， 1则商户扫码后，手机端仅展示公众号、2表示仅展示小程序，3表示公众号和小程序都展示。如果为未制定，则默认小程序和公众号都展示。</param>
        public static string GetBindUrl(string componentAppId, string preAuthCode, string redirectUri, bool useMobile = false, int authType = 3)
        {
            if (useMobile)
            {
                return string.Format(_getBindUrlLink, authType, componentAppId, preAuthCode, redirectUri);
            }
            else
            {
                return string.Format(_getBindUrlQrcode, authType, componentAppId, preAuthCode, redirectUri);
            }
        }
    }
}
