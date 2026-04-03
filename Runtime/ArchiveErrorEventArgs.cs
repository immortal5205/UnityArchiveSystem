using System;
namespace NuoYan.Archive
{
    /// <summary>
    /// 存档错误事件参数
    /// </summary>
    public class ArchiveErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 错误类型
        /// </summary>
        public ArchiveErrorType ErrorType { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// 异常对象
        /// </summary>
        public Exception Exception { get; set; }
    }
}


