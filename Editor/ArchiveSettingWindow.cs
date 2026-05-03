#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace NuoYan.Archive
{
    /// <summary>
    /// 存档设置窗口
    /// </summary>
    public class ArchiveSettingWindow : EditorWindow
    {
        [MenuItem("Tools/ArchiveSetting")]
        private static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ArchiveSettingWindow), false, "ArchiveSetting");
        }
        private void OnGUI()
        {
            if (ArchiveSetting.Instance == null)
            {
                EditorGUILayout.HelpBox("No ArchiveSetting asset found. Please create one in the Resources folder.", MessageType.Warning);
                return;
            }
            EditorGUILayout.LabelField("Archive Setting", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            //绘制枚举
            ArchiveSetting.Instance.ArchiveType = (ArchiveType)EditorGUILayout.EnumPopup("Archive Type", ArchiveSetting.Instance.ArchiveType);
            ArchiveSetting.Instance.EncryptionType = (EncryptionType)EditorGUILayout.EnumPopup("Encryption Type", ArchiveSetting.Instance.EncryptionType);
            if (ArchiveSetting.Instance.EncryptionType == EncryptionType.AES)
            {
                ArchiveSetting.Instance.KEY = EditorGUILayout.TextField("Encryption Key", ArchiveSetting.Instance.KEY);
                EditorGUILayout.BeginHorizontal();
                {
                    ArchiveSetting.Instance.IV = EditorGUILayout.TextField("Encryption IV", ArchiveSetting.Instance.IV);
                    if (GUILayout.Button("Generate IV", GUILayout.MaxWidth(100)))
                    {
                        ArchiveSetting.Instance.IV = Rijindael.GenerateIV();
                    }
                }
                EditorGUILayout.EndHorizontal();

            }
            ArchiveSetting.Instance.FolderName = EditorGUILayout.TextField("Folder Name", ArchiveSetting.Instance.FolderName);
            if (ArchiveSetting.Instance.ArchiveType == ArchiveType.Single)
            {
                ArchiveSetting.Instance.FileName = EditorGUILayout.TextField("File Name", ArchiveSetting.Instance.FileName);
            }
            ArchiveSetting.Instance.FileType = (FileType)EditorGUILayout.EnumPopup("File Type", ArchiveSetting.Instance.FileType);
            ArchiveSetting.Instance.ArchiveDirection = (ArchiveDirection)EditorGUILayout.EnumPopup("Archive Direction", ArchiveSetting.Instance.ArchiveDirection);

            if (GUILayout.Button("Open Path"))
            {
                ArchiveSetting.Instance.OpenPath();
            }
            if (GUILayout.Button("Clear Data"))
            {
                if (EditorUtility.DisplayDialog("Clear Data", "Are you sure you want to clear all data?", "Yes", "No"))
                {
                    ArchiveSetting.Instance.Clear();
                }
            }
        }
    }
}
#endif