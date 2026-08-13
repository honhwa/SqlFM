using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace SqlFM.Localization
{
    /// <summary>
    /// 创建支持运行时本地化的 OleMenuCommand：
    /// 注册时按当前语言设置文本，并订阅语言切换事件自动刷新。
    /// </summary>
    public static class CommandLocalizer
    {
        /// <summary>创建本地化菜单命令。</summary>
        /// <param name="commandId">命令 ID</param>
        /// <param name="invokeHandler">执行回调</param>
        /// <param name="locKey">StringTable 中的文本 key</param>
        public static OleMenuCommand Create(CommandID commandId, EventHandler invokeHandler, string locKey)
        {
            var command = new OleMenuCommand(invokeHandler, commandId);
            command.Text = Localizer.Get(locKey);
            Localizer.Instance.LanguageChanged += (s, e) => command.Text = Localizer.Get(locKey);
            return command;
        }
    }
}
