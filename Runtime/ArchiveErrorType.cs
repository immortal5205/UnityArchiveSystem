namespace NuoYan.Archive
{
    /// <summary>
    /// 存档错误类型
    /// </summary>
    public enum ArchiveErrorType
    {
        /// <summary>
        /// 文件夹创建失败
        /// </summary>
        FolderCreationFailed,
        /// <summary>
        /// 存档文件不存在
        /// </summary>
        FileNotFound,
        /// <summary>
        /// 存档失败
        /// </summary>
        SaveFailed,
        /// <summary>
        /// 加载失败
        /// </summary>
        LoadFailed,
        /// <summary>
        /// 删除失败
        /// </summary>
        DeleteFailed,
        /// <summary>
        /// 其他错误
        /// </summary>
        Other
    }
}


