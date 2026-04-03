
using System;
namespace NuoYan.Archive
{
    /// <summary>
    /// 存档数据基类
    /// </summary>
    [Serializable]
    public class ArchiveDataBase
    {
        /// <summary>
        /// 存档时间戳
        /// </summary>
        public long TimeStamp { get; set; }

        public ArchiveDataBase()
        {

        }
    }
}