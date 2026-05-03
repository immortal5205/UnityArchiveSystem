#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace NuoYan.Archive
{
    [CustomEditor(typeof(ArchiveSystem<,>), true)]
    public class ArchiveSystemEditor1 : Editor
    {
        private SerializedProperty m_CurrentArchiveData;
        private SerializedProperty m_ArchiveDataList;
        private SerializedProperty m_ArchiveTableList;
        private ArchiveSetting m_ArchiveSetting => ArchiveSetting.Instance;
        private void OnEnable()
        {
            m_CurrentArchiveData = serializedObject.FindProperty("m_CurrentArchiveData");
            m_ArchiveDataList = serializedObject.FindProperty("m_ArchiveDataList");
            m_ArchiveTableList = serializedObject.FindProperty("m_ArchiveTableList");
        }
        public override void OnInspectorGUI()
        {
            switch (m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    EditorGUILayout.HelpBox("当前存档类型：单存档\n将覆盖之前的存档数据", MessageType.Info);
                    break;
                case ArchiveType.Multiple:
                    EditorGUILayout.HelpBox("当前存档类型：多存档\n每次保存都会生成新的存档数据", MessageType.Info);
                    break;
                default:
                    break;
            }
            EditorGUI.BeginDisabledGroup(true);
            if (m_ArchiveSetting.ArchiveType == ArchiveType.Multiple)
            {
                EditorGUILayout.PropertyField(m_ArchiveDataList);
                EditorGUILayout.PropertyField(m_ArchiveTableList);
            }
            EditorGUILayout.PropertyField(m_CurrentArchiveData);

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            GUILayout.Label("存档管理", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (GUILayout.Button("打开存档路径"))
            {
                m_ArchiveSetting.OpenPath();
            }

            if (GUILayout.Button("清空存档"))
            {
                if (EditorUtility.DisplayDialog("警告", "确定要清空所有存档吗？此操作不可恢复。", "确定", "取消"))
                {
                    m_ArchiveSetting.Clear();
                    Debug.Log("存档已清空");
                }
            }

            GUILayout.EndHorizontal();
        }
    }
    [CustomEditor(typeof(ArchiveSystem<,,>), true)]
    public class ArchiveSystemEditor2 : Editor
    {
        private SerializedProperty m_CurrentArchiveData1;
        private SerializedProperty m_CurrentArchiveData2;
        private SerializedProperty m_ArchiveDataList1;
        private SerializedProperty m_ArchiveDataList2;
        private SerializedProperty m_ArchiveTableList;
        private ArchiveSetting m_ArchiveSetting => ArchiveSetting.Instance;
        private void OnEnable()
        {
            m_CurrentArchiveData1 = serializedObject.FindProperty("m_CurrentArchiveData1");
            m_CurrentArchiveData2 = serializedObject.FindProperty("m_CurrentArchiveData2");
            m_ArchiveDataList1 = serializedObject.FindProperty("m_ArchiveDataList1");
            m_ArchiveDataList2 = serializedObject.FindProperty("m_ArchiveDataList2");
            m_ArchiveTableList = serializedObject.FindProperty("m_ArchiveTableList");
        }
        public override void OnInspectorGUI()
        {
            switch (m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    EditorGUILayout.HelpBox("当前存档类型：单存档\n将覆盖之前的存档数据", MessageType.Info);
                    break;
                case ArchiveType.Multiple:
                    EditorGUILayout.HelpBox("当前存档类型：多存档\n每次保存都会生成新的存档数据", MessageType.Info);
                    break;
                default:
                    break;
            }
            EditorGUI.BeginDisabledGroup(true);
            if (m_ArchiveSetting.ArchiveType == ArchiveType.Multiple)
            {
                EditorGUILayout.PropertyField(m_ArchiveDataList1);
                EditorGUILayout.PropertyField(m_ArchiveDataList2);
                EditorGUILayout.PropertyField(m_ArchiveTableList);
            }
            EditorGUILayout.PropertyField(m_CurrentArchiveData1);
            EditorGUILayout.PropertyField(m_CurrentArchiveData2);

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            GUILayout.Label("存档管理", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (GUILayout.Button("打开存档路径"))
            {
                m_ArchiveSetting.OpenPath();
            }

            if (GUILayout.Button("清空存档"))
            {
                if (EditorUtility.DisplayDialog("警告", "确定要清空所有存档吗？此操作不可恢复。", "确定", "取消"))
                {
                    m_ArchiveSetting.Clear();
                    Debug.Log("存档已清空");
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}
#endif