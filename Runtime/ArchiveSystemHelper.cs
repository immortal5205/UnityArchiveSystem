using UnityEngine;
using System.IO;
using System;
namespace NuoYan.Archive
{
    /// <summary>
    /// 存档系统辅助类
    /// </summary>
    public static class ArchiveSystemHelper
    {
        /// <summary>
        /// 获取子文件夹路径
        /// </summary>
        /// <param name="mainFolderPath">主文件夹路径</param>
        /// <param name="folderType">文件夹类型</param>
        /// <returns>子文件夹路径</returns>
        public static string GetChildFolder(string mainFolderPath, Type folderType)
        {
            string fn = folderType.Name;
            if (typeof(IArhivePathPar).IsAssignableFrom(folderType))
            {
                var pathPar = (IArhivePathPar)Activator.CreateInstance(folderType);
                if (string.IsNullOrEmpty(pathPar.DataPath))
                {
                    Debug.LogWarning($"存档数据 {folderType.Name}实现了IPathPar接口但未设置DataPath属性，默认使用类名作为子文件夹名称");
                }
                else
                {
                    fn = pathPar.DataPath;
                }
            }
            string childFolderPath = Path.Combine(mainFolderPath, fn);
            try
            {
                if (!Directory.Exists(childFolderPath))
                {
                    Directory.CreateDirectory(childFolderPath);
                }
                return childFolderPath;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"创建子文件夹失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 尝试创建主文件夹
        /// </summary>
        /// <param name="archiveSetting">存档设置</param>
        /// <param name="path">输出的路径</param>
        /// <returns>是否创建成功</returns>
        public static bool TryCreateMainFolderFolder(ArchiveSetting archiveSetting, out string path)
        {
            path = "";
            try
            {
                switch (archiveSetting.ArchiveDirection)
                {
                    case ArchiveDirection.Persistent:
                        path = Path.Combine(Application.persistentDataPath, archiveSetting.FolderName);
                        break;
                    case ArchiveDirection.Application:
                        path = Path.Combine(Application.dataPath.Replace("Assets", ""), archiveSetting.FolderName);
                        break;
                }
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return Directory.Exists(path);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"创建文件夹失败: {ex.Message}");
                return false;
            }
        }
        public static SerializeVetor3 ToSerialize(this Vector3 vector)
        {
            return new SerializeVetor3(vector);
        }
        public static SerializeVector2 ToSerialize(this Vector2 vector)
        {
            return new SerializeVector2(vector);
        }
        public static SerializeQuaternion ToSerialize(this Quaternion quaternion)
        {
            return new SerializeQuaternion(quaternion);
        }
        public static SerializeVector3Int ToSerialize(this Vector3Int vector)
        {
            return new SerializeVector3Int(vector);
        }
        public static SerializeVector2Int ToSerialize(this Vector2Int vector)
        {
            return new SerializeVector2Int(vector);
        }
        public static SerializeTransform ToSerialize(this Transform transform)
        {
            return new SerializeTransform(transform);
        }
        public static SerializeRectTransform ToSerialize(this RectTransform rect)
        {
            return new SerializeRectTransform(rect);
        }
        public static SerializeColor ToSerialize(this Color color)
        {
            return new SerializeColor(color);
        }
    }
}


