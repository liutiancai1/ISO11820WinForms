using ISO11820_2020.Models;
using ISO11820WinForms.Models;
using TestServer.Models;

namespace ISO11820WinForms.Global
{
    public class AppGlobalWinForms
    {
        //试验设备信息全局缓存
        public Dictionary<int, Apparatus> DictApparatus { get; set; }

        //构造函数
        public AppGlobalWinForms()
        {
            DictApparatus = new Dictionary<int, Apparatus>();
            //添加试验设备对象至全局缓存
            using (var ctx = new ISO11820DbContext())
            {
                DictApparatus = ctx.Apparatuses.ToDictionary(x => x.Apparatusid);
            }
        }
    }
}
