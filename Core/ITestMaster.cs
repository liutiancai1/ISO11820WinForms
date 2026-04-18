using ISO11820_2020.Models;
using TestServer.Models;

namespace ISO11820WinForms.Core
{
    public interface ITestMaster
    {
        //初始化完成后需要执行的操作
        public void OnInitialized();

        /* =====================试验控制器任务接口方法========================= */
        /* 新建试验任务 */
        public void CreateTest();
        /* 新建系统标定任务 */
        public void CreateCalibration();

        /* =====================试验控制器工作状态变更接口方法========================= */
        /* 开始记录数据 */
        public bool StartRecording();
        /* 停止记录数据 */
        public bool StopRecording();

        /* =====================试验控制器状态判定接口方法========================= */
        /* 
         * 功能: 判断试验初始条件是否满足 
         * 返回:
         *       true  - 控制器已达到试验初始条件
         *       false - 控制器未达到试验初始条件
         */
        public bool CheckStartCriteria();

        /* 
         * 功能: 判断试验终止条件是否满足 
         * 返回:
         *       true  - 控制器已达到试验终止条件
         *       false - 控制器未达到试验终止条件
         */
        public bool CheckTerminateCriteria();

        /* =====================试验控制器本次试验数据设置及处理的接口方法========================= */
        /*
         * 功能: 更新本次试验现象记录
         * 参数:
         *      phenocode - 主要现象编码
         *      memo      - 其他现象文字描述
         */
        public void SetPhenomenon(string phenocode, string memo);

        /*
         * 功能: 设置本次试验样品的产品信息
         * 参数:
         *       proddata - 本次试验样品的产品数据缓存对象
         */
        public void SetProductData(Productmaster prodmaster);

        /*
         * 功能: 获取本次试样样品的产品信息
         * 返回:
         *      本次试验样品的产品数据缓存对象
         */
        public Productmaster GetProductData();

        /*
         * 功能: 重置本次试样样品的产品信息
         */
        public void ResetProductData();

        /*
         * 功能: 设置本次试验数据缓存对象
         * 参数:
         *       testdata - 本次试验数据缓存对象
         */
        public void SetTestData(Testmaster testmaster);

        /*
         * 功能: 获取本次试验数据缓存对象
         * 返回:
         *      本次试验数据缓存对象
         */
        public Testmaster GetTestData();

        /*
         * 功能: 重置本次试验数据缓存对象
         */
        public void ResetTestData();

        /* 试验结束后期处理(保存本次试验数据,视频记录等操作) */
        public Task PostTestProcess();
    }
}
