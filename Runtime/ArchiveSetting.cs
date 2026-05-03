using UnityEngine;
namespace NuoYan.Archive
{
    [CreateAssetMenu(fileName = "ArchiveSetting", menuName = "ArchiveSystem/ArchiveSetting")]
    public class ArchiveSetting : ScriptableObject
    {
        private static ArchiveSetting _instance;
        public static ArchiveSetting Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ArchiveSetting>("ArchiveSetting");
                    if (_instance == null)
                    {
                        Debug.LogWarning("未找到ArchiveSetting资源，创建默认实例");
                        _instance = ScriptableObject.CreateInstance<ArchiveSetting>();
                    }
                }
                return _instance;
            }
        }
        public const string ARCHIVETABLE = "ArchiveTable.json";

        public ArchiveType ArchiveType = ArchiveType.Single;
        public EncryptionType EncryptionType = EncryptionType.None;
        /// <summary>
        /// AES加密的初始向量（IV），必须是16字节（128位）长度的Base64字符串。
        /// 例如："Rkb4jvUy/ye7Cd7k89QQgQ=="，这是一个随机生成的16字节IV的Base64编码
        /// </summary> 
        [Tooltip("AES加密的初始向量（IV），必须是16字节（128位）长度的Base64字符串。例如：\"Rkb4jvUy/ye7Cd7k89QQgQ==\"，这是一个随机生成的16字节IV的Base64编码")]
        public string IV = "Rkb4jvUy/ye7Cd7k89QQgQ==";
        [Tooltip("任意长度密钥")]
        public string KEY = "ArchiveSystem";
        public string FolderName = "MySave";
        public string FileName = "SaveFile.sav";
        public FileType FileType = FileType.Json;
        public ArchiveDirection ArchiveDirection = ArchiveDirection.Persistent;
        public void OpenPath()
        {
            switch (ArchiveDirection)
            {
                case ArchiveDirection.Persistent:
                    Application.OpenURL(Application.persistentDataPath + "/" + FolderName);
                    break;
                case ArchiveDirection.Application:
                    Application.OpenURL(Application.dataPath.Replace("Assets", "") + "/" + FolderName);
                    break;
                default:
                    break;
            }
        }
        public void Clear()
        {
            switch (ArchiveDirection)
            {
                case ArchiveDirection.Persistent:
                    System.IO.Directory.Delete(Application.persistentDataPath + "/" + FolderName, true);
                    break;
                case ArchiveDirection.Application:
                    System.IO.Directory.Delete(Application.dataPath.Replace("Assets", "") + "/" + FolderName, true);
                    break;
                default:
                    break;
            }
        }

    }
    public enum FileType
    {
        Json,
    }
    public enum ArchiveDirection
    {
        Persistent,
        Application
    }
    public enum ArchiveType
    {
        Single,
        Multiple
    }
    public enum EncryptionType
    {
        None = 0,
        AES = 1,
    }
}
