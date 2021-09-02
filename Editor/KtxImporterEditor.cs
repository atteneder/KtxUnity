using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace KtxUnity
{
    [CustomEditor(typeof(KtxImporter))]
    public class KtxImporterEditor : ScriptedImporterEditor
    {
        private SerializedProperty m_ReportItems;
        
        public override void OnEnable()
        {
            base.OnEnable();
            m_ReportItems = serializedObject.FindProperty("reportItems");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var reportItemCount = m_ReportItems.arraySize;
            for (int i = 0; i < reportItemCount; i++)
            {
                EditorGUILayout.HelpBox(m_ReportItems.GetArrayElementAtIndex(i).stringValue, MessageType.Error);
            }
            
            ApplyRevertGUI();
        }
    }
}