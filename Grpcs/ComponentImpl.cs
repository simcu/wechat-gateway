using Grpcs.Gateway.Wechat;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Grpc.Core;
using Wechat.Helpers;
using Wechat.Api;

namespace Wechat.Grpcs
{
    public class ComponentImpl : Component.ComponentBase
    {
        IDatabase _redis { get; }
        IConfiguration _config { get; }
        string _componentAppId
        {
            get
            {
                return _config["Wechat:AppID"];
            }
        }
        string _componentAccessToken
        {
            get
            {
                return _redis.StringGet(CacheKey.ComponentAccessToken);
            }
        }

        public ComponentImpl(IDatabase redis, IConfiguration config)
        {
            _redis = redis;
            _config = config;
        }

        public override Task<GetBindUrlResponse> GetBindUrl(GetBindUrlRequest request, ServerCallContext context)
        {
            var resp = new GetBindUrlResponse();
            //获取preauthcode
            var preCode = ComponentApi.GetPreAuthCode(_componentAccessToken, _config["Wechat:AppID"]);
            if (preCode.ErrCode == 0)
            {
                if (request.UseMobile)
                {
                    resp.Url = ComponentApi.GetBindUrl(_componentAppId, preCode.PreAuthCode, request.RedirectUrl, true);
                }
                else
                {
                    resp.Url = ComponentApi.GetBindUrl(_componentAppId, preCode.PreAuthCode, request.RedirectUrl);
                }
            }
            else
            {
                resp.Error = new Error
                {
                    ErrCode = preCode.ErrCode,
                    ErrMsg = preCode.ErrMsg
                };
            }

            return Task.FromResult(resp);
        }

        public override Task<GetInfoResponse> GetInfo(GetInfoRequest request, ServerCallContext context)
        {
            var resp = new GetInfoResponse();

            var data = ComponentApi.GetAuthorizerInfo(_componentAccessToken, _componentAppId, request.AppId);
            if (data.ErrCode == 0)
            {
                _redis.StringSet(CacheKey.UserRefreshTokenPrefix + data.AuthorizationInfo.AuthorizerAppId, data.AuthorizationInfo.AuthorizerRefreshToken);
                if (data.AuthorizerInfo.MiniProgramInfo != null)
                {
                    _redis.StringSet(CacheKey.UserIsWxAppPrefix + data.AuthorizationInfo.AuthorizerAppId, 1);
                    resp.Type = "WxApp";
                }
                else
                {
                    _redis.KeyDelete(CacheKey.UserIsWxAppPrefix + data.AuthorizationInfo.AuthorizerAppId);
                    resp.Type = "WxWeb";
                }
                resp.HeadImg = data.AuthorizerInfo.HeadImg;
                resp.NickName = data.AuthorizerInfo.NickName;
                resp.PrincipalName = data.AuthorizerInfo.PrincipalName;
                resp.UserName = data.AuthorizerInfo.UserName;
                resp.Alias = data.AuthorizerInfo.Alias;
                resp.AppId = data.AuthorizationInfo.AuthorizerAppId;
                resp.QrcodeUrl = data.AuthorizerInfo.QrcodeUrl;
                resp.ServiceTypeInfo = data.AuthorizerInfo.ServiceTypeInfo.Id;
                resp.VerifyTypeInfo = data.AuthorizerInfo.VerifyTypeInfo.Id;
                foreach (var item in data.AuthorizationInfo.FuncInfos)
                {
                    resp.Permissions.Add(item.Category.Id);
                }
            }
            else
            {
                resp.Error = new Error
                {
                    ErrMsg = data.ErrMsg,
                    ErrCode = data.ErrCode
                };
            }
            return Task.FromResult(resp);
        }

        public override Task<GetAppIdResponse> GetAppId(GetAppIdRequest request, ServerCallContext context)
        {
            var resp = new GetAppIdResponse();
            var data = ComponentApi.GetAuthInfo(_componentAccessToken, _componentAppId, request.Code);
            if (data.ErrCode == 0)
            {
                resp.AppId = data.AuthorizationInfo.AuthorizerAppId;
            }
            else
            {
                resp.Error = new Error
                {
                    ErrCode = data.ErrCode,
                    ErrMsg = data.ErrMsg
                };
            }
            return Task.FromResult(resp);
        }
    }

}
