#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace NuoYan.Archive
{
    [CustomEditor(typeof(ArchiveSetting))]
    public class ArchiveSettingEditor : Editor
    {
        private SerializedProperty m_ArchiveType;
        private SerializedProperty m_FolderName;
        private SerializedProperty m_FileName;
        private SerializedProperty m_FileType;
        private SerializedProperty m_ArchiveDirection;
        private SerializedProperty m_EncryptionType;
        private SerializedProperty m_KEY;
        private SerializedProperty m_IV;
        void OnEnable()
        {
            m_ArchiveType = serializedObject.FindProperty("ArchiveType");
            m_FolderName = serializedObject.FindProperty("FolderName");
            m_FileName = serializedObject.FindProperty("FileName");
            m_FileType = serializedObject.FindProperty("FileType");
            m_ArchiveDirection = serializedObject.FindProperty("ArchiveDirection");
            m_EncryptionType = serializedObject.FindProperty("EncryptionType");
            m_KEY = serializedObject.FindProperty("KEY");
            m_IV = serializedObject.FindProperty("IV");

        }
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Please do not delete this file, otherwise the archive system will not work properly", MessageType.Info);
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ArchiveType);
            EditorGUILayout.PropertyField(m_EncryptionType);
            if (m_EncryptionType.enumValueIndex == (int)EncryptionType.AES)
            {
                EditorGUILayout.PropertyField(m_KEY);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(m_IV);
                    if (GUILayout.Button("Generate IV", GUILayout.MaxWidth(100)))
                    {
                        m_IV.stringValue = Rijindael.GenerateIV();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.PropertyField(m_FolderName);
            if (m_ArchiveType.enumValueIndex == (int)ArchiveType.Single)
            {
                EditorGUILayout.PropertyField(m_FileName);
            }
            EditorGUILayout.PropertyField(m_FileType);
            EditorGUILayout.PropertyField(m_ArchiveDirection);

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
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif