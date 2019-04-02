using G = Grpcs.Gateway.Wechat;
using Grpcs.Gateway.Wechat;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Grpc.Core;
using Wechat.Helpers;
using Wechat.Api;
using System;
using System.IO;
namespace Wechat.Grpcs
{
    public class WxWebImpl : WxWeb.WxWebBase
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

        public WxWebImpl(IDatabase redis, IConfiguration config)
        {
            _redis = redis;
            _config = config;
        }

        public override Task<GetAuthCodeUrlResponse> GetAuthCodeUrl(GetAuthCodeUrlRequest request, ServerCallContext context)
        {
            var resp = new GetAuthCodeUrlResponse
            {
                Url = WxWebApi.GetCodeUrl(_componentAppId, request.AppId, request.RedirectUrl)
            };
            return Task.FromResult(resp);
        }

        public override Task<GetOpenIdByCodeResponse> GetOpenId(GetOpenIdByCodeRequest request, ServerCallContext context)
        {
            var resp = new GetOpenIdByCodeResponse();
            var data = WxWebApi.GetAccessToken(_componentAppId, _componentAccessToken, request.AppId, request.Code);
            if (data.ErrCode == 0)
            {
                resp.OpenId = data.OpenId;
                resp.SecretKey = data.AccessToken;
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

        public override Task<GetUserInfoResponse> GetUserInfo(GetUserInfoRequest request, ServerCallContext context)
        {
            var resp = new GetUserInfoResponse();
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + request.AppId);
            if (accessToken.HasValue)
            {
                var data = WxWebApi.GetUserInfo(accessToken, request.OpenId);
                if (data.ErrCode == 0)
                {
                    resp.SubscribeTime = data.Subscribe == 1 ? data.SubscribeTime : 0;
                    resp.OpenId = data.OpenId;
                    if (resp.SubscribeTime != 0)
                    {
                        resp.NickName = data.NickName;
                        resp.Sex = data.Sex;
                        resp.City = data.City;
                        resp.Province = data.Province;
                        resp.Country = data.Country;
                        resp.Photo = data.HeadImgUrl;
                        resp.UnionId = data.UnionId;
                    }
                }
                else
                {
                    resp.Error = new Error
                    {
                        ErrCode = data.ErrCode,
                        ErrMsg = data.ErrMsg
                    };
                }
            }
            else
            {
                resp.Error = new Error
                {
                    ErrCode = 99999,
                    ErrMsg = "AccessToken Missing"
                };
            }
            return Task.FromResult(resp);
        }

        public override Task<GetJsTicketResponse> GetJsTicket(GetJsTicketRequest request, ServerCallContext context)
        {
            var resp = new GetJsTicketResponse();
            var jsTicket = _redis.StringGet(CacheKey.UserJsTicketPrefix + request.AppId);
            if (jsTicket.HasValue)
            {
                resp.JsTicket = jsTicket;
            }
            else
            {
                resp.Error = new Error
                {
                    ErrCode = 99999,
                    ErrMsg = "JsTicket Missing"
                };
            }
            return Task.FromResult(resp);
        }

        public override Task<GetQrCodeResponse> GetQrCode(GetQrCodeRequest request, ServerCallContext context)
        {
            var resp = new GetQrCodeResponse();
            var qrCodeUrl = "https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=";
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + request.AppId);
            if (accessToken.HasValue)
            {
                var result = WxWebApi.CreateQrCode(accessToken, request.Data);
                if (result.ErrCode == 0)
                {
                    resp.Expired = Util.GetTimestamp() + result.ExpireSeconds;
                    resp.ImageUrl = result.Url;
                    resp.QrcodeUrl = qrCodeUrl + result.Ticket;
                }
                else
                {
                    resp.Error = new Error
                    {
                        ErrCode = result.ErrCode,
                        ErrMsg = result.ErrMsg
                    };
                }
            }
            else
            {
                resp.Error = new Error
                {
                    ErrCode = 99999,
                    ErrMsg = "AccessToken Missing"
                };
            }
            return Task.FromResult(resp);
        }

        public override Task<Error> CreateMenu(CreateMenuRequest request, ServerCallContext context)
        {
            var resp = new Error();
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + request.AppId);
            if (accessToken.HasValue)
            {
                var data = WxWebApi.SetMenu(accessToken, request.Config);
                if (data.ErrCode != 0)
                {
                    resp.ErrCode = data.ErrCode;
                    resp.ErrMsg = data.ErrMsg;
                }
            }
            else
            {
                resp.ErrCode = 99999;
                resp.ErrMsg = "AccessToken Missing";
            }
            return Task.FromResult(resp);
        }

        public override Task<Error> DeleteMenu(GetInfoRequest request, ServerCallContext context)
        {
            var resp = new Error();
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + request.AppId);
            if (accessToken.HasValue)
            {
                var data = WxWebApi.DeleteMenu(accessToken);
                if (data.ErrCode != 0)
                {
                    resp.ErrCode = data.ErrCode;
                    resp.ErrMsg = data.ErrMsg;
                }
            }
            else
            {
                resp.ErrCode = 99999;
                resp.ErrMsg = "AccessToken Missing";
            }
            return Task.FromResult(resp);
        }

        public override Task<G.GetImageListResponse> GetImageList(G.GetImageListRequest request, ServerCallContext context)
        {
            var resp = new G.GetImageListResponse();
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + request.AppId);
            if (accessToken.HasValue)
            {
                var result = WxWebApi.GetImageList(accessToken, request.Page);
                if (result.ErrCode == 0)
                {
                    resp.Total = result.TotalCount;
                    foreach (var item in result.List)
                    {
                        resp.List.Add(new ImageItem
                        {
                            ImageId = item.MediaId,
                            UpdateTime = item.UpdateTime,
                            Url = item.Url
                        });
                    }
                }
                else
                {
                    resp.Error = new Error
                    {
                        ErrCode = result.ErrCode,
                        ErrMsg = result.ErrMsg
                    };
                }
            }
            else
            {
                resp.Error = new Error
                {
                    ErrCode = 99999,
                    ErrMsg = "AccessToken Missing"
                };
            }
            return Task.FromResult(resp);
        }

        public override Task<Error> DeleteImage(G.DeleteImageRequest request, ServerCallContext context)
        {
            var resp = new Error();
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + request.AppId);
            if (accessToken.HasValue)
            {
                var result = WxWebApi.DeleteImage(accessToken, request.ImageId);
                if (result.ErrCode != 0)
                {
                    resp.ErrCode = result.ErrCode;
                    resp.ErrMsg = result.ErrMsg;
                }
            }
            else
            {
                resp.ErrCode = 99999;
                resp.ErrMsg = "AccessToken Missing";
            }
            return Task.FromResult(resp);
        }

        public override Task<UploadImageResponse> UploadImage(UploadImageRequest request, ServerCallContext context)
        {
            var resp = new UploadImageResponse();
            var accessToken = _redis.StringGet(CacheKey.UserAccessTokenPrefix + request.AppId);
            if (accessToken.HasValue)
            {
                var result = WxWebApi.UploadImage(accessToken, request.File.ToByteArray());
                if (result.ErrCode != 0)
                {
                    resp.Error = new Error
                    {
                        ErrCode = result.ErrCode,
                        ErrMsg = result.ErrMsg
                    };
                }
                else
                {
                    resp.ImageId = result.MediaId;
                    resp.Url = result.Url;
                }
            }
            else
            {
                resp.Error = new Error
                {
                    ErrCode = 99999,
                    ErrMsg = "AccessToken Missing"
                };
            }
            return Task.FromResult(resp);
        }
    }
}
