using System;
using Microsoft.VisualStudio.Shell;

namespace SqlFM.Editor
{
    /// <summary>
    /// 编辑器辅助类，负责获取和替换 SSMS 查询编辑器中的文本。
    /// 通过 EnvDTE 自动化模型与编辑器交互。
    /// </summary>
    public static class EditorHelper
    {
        /// <summary>
        /// 获取当前活动文档的选中文本。
        /// 如果没有选中内容，返回 null。
        /// </summary>
        /// <param name="dte">DTE2 自动化对象实例</param>
        /// <returns>选中的文本内容；无选中时返回 null</returns>
        public static string? GetSelectedText(EnvDTE80.DTE2 dte)
        {
            // 确保在 UI 线程上执行，EnvDTE 操作需要在 UI 线程
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!HasActiveTextDocument(dte))
            {
                return null;
            }

            try
            {
                // 获取当前文档的 TextDocument 对象
                var textDoc = (EnvDTE.TextDocument)dte.ActiveDocument!.Object("TextDocument")!;
                var selection = textDoc.Selection;

                // 判断是否有选中内容
                if (selection == null || selection.IsEmpty)
                {
                    return null;
                }

                return selection.Text;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSelectedText 异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取当前活动文档的全部文本。
        /// </summary>
        /// <param name="dte">DTE2 自动化对象实例</param>
        /// <returns>文档全部文本内容；失败时返回 null</returns>
        public static string? GetAllText(EnvDTE80.DTE2 dte)
        {
            // 确保在 UI 线程上执行
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!HasActiveTextDocument(dte))
            {
                return null;
            }

            try
            {
                // 获取 TextDocument 对象
                var textDoc = (EnvDTE.TextDocument)dte.ActiveDocument!.Object("TextDocument")!;

                // 使用 EditPoint 获取从文档起点到终点的全部文本
                // CreateEditPoint 创建可移动的编辑点，不会改变文档内容
                EnvDTE.EditPoint startPoint = textDoc.StartPoint.CreateEditPoint();
                string allText = startPoint.GetText(textDoc.EndPoint);

                return allText;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllText 异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 替换当前选中的文本。
        /// </summary>
        /// <param name="dte">DTE2 自动化对象实例</param>
        /// <param name="newText">替换后的新文本</param>
        public static void ReplaceSelectedText(EnvDTE80.DTE2 dte, string newText)
        {
            // 确保在 UI 线程上执行
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!HasActiveTextDocument(dte))
            {
                return;
            }

            try
            {
                // 获取 TextDocument 和当前选中区域
                var textDoc = (EnvDTE.TextDocument)dte.ActiveDocument!.Object("TextDocument")!;
                var selection = textDoc.Selection;

                if (selection == null || selection.IsEmpty)
                {
                    return;
                }

                // 使用 Insert 方法替换选中的文本
                // Insert 会先删除选中内容，再插入新文本
                selection.Insert(newText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReplaceSelectedText 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 替换当前文档的全部文本。
        /// </summary>
        /// <param name="dte">DTE2 自动化对象实例</param>
        /// <param name="newText">替换后的新文本</param>
        public static void ReplaceAllText(EnvDTE80.DTE2 dte, string newText)
        {
            // 确保在 UI 线程上执行
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!HasActiveTextDocument(dte))
            {
                return;
            }

            try
            {
                // 获取 TextDocument 对象
                var textDoc = (EnvDTE.TextDocument)dte.ActiveDocument!.Object("TextDocument")!;

                // 使用 EditPoint 的 ReplaceText 方法替换全部文本
                // 创建起点的 EditPoint，替换到终点的所有内容
                EnvDTE.EditPoint startPoint = textDoc.StartPoint.CreateEditPoint();
                startPoint.ReplaceText(textDoc.EndPoint, newText,
                    (int)EnvDTE.vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReplaceAllText 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查当前是否有活动的文本文档。
        /// </summary>
        /// <param name="dte">DTE2 自动化对象实例</param>
        /// <returns>存在活动文本文档时返回 true</returns>
        public static bool HasActiveTextDocument(EnvDTE80.DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // 检查是否有活动文档，且文档类型为文本
                return dte.ActiveDocument != null
                    && !string.IsNullOrEmpty(dte.ActiveDocument.FullName);
            }
            catch
            {
                // 当没有活动文档时，访问 ActiveDocument 可能抛出异常
                return false;
            }
        }
    }
}
