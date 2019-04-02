using System;
namespace Wechat.Helpers
{
    public static class Util
    {
        //获取秒时间戳
        public static int GetTimestamp()
        {
            return int.Parse(((DateTime.UtcNow.Ticks - 621355968000000000) / 10000000).ToString());
        }
    }
}
