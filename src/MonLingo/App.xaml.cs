using System.Windows;

namespace MonLingo
{
    /// <summary>
    /// MonLingo 應用程式主入口點.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 應用程式啟動事件處理.
        /// </summary>
        /// <param name="e">啟動事件參數.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // TODO: 初始化依賴注入容器
            // TODO: 註冊服務
            // TODO: 初始化配置
        }

        /// <summary>
        /// 應用程式退出事件處理.
        /// </summary>
        /// <param name="e">退出事件參數.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            // TODO: 清理資源
            base.OnExit(e);
        }
    }
}
