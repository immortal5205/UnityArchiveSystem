using System;
namespace NuoYan.Archive
{
    /// <summary>
    /// 存档目录基类
    /// </summary>
    [Serializable]
    public class ArchiveTableBase
    {
        /// <summary>
        /// 存档时间戳
        /// </summary>
        public long TimeStamp { get; set; }
        public bool IsLoaded { get; set; }

        public ArchiveTableBase()
        {
            TimeStamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
    }
}