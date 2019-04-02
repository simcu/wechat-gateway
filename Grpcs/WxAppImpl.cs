using Grpcs.Gateway.Wechat;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Grpc.Core;
using Wechat.Helpers;
using Wechat.Api;
namespace Wechat.Grpcs
{
    public class WxAppImpl : WxApp.WxAppBase
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

        public WxAppImpl(IDatabase redis, IConfiguration config)
        {
            _redis = redis;
            _config = config;
        }

        public override Task<GetOpenIdByCodeResponse> GetOpenId(GetOpenIdByCodeRequest request, ServerCallContext context)
        {
            var resp = new GetOpenIdByCodeResponse();
            var data = WxAppApi.GetSessionKey(_componentAppId, _componentAccessToken, request.AppId, request.Code);
            if (data.ErrCode == 0)
            {
                resp.OpenId = data.OpenId;
                resp.SecretKey = data.SessionKey;
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
