using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AAZ.Core.Editor
{
    /// <summary>
    /// Inspector for <see cref="RandomTable"/> that surfaces transcription errors as you type.
    /// Copying a table out of a rulebook is where mistakes get made, and a gap in the ranges
    /// otherwise stays invisible until a roll lands on it mid-session and returns nothing.
    /// </summary>
    [CustomEditor(typeof(RandomTable))]
    public sealed class RandomTableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var table = (RandomTable)target;

            EditorGUILayout.Space();

            List<string> problems = table.Validate();
            if (problems.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"Table is complete: every {table.die} result maps to exactly one row.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(string.Join("\n", problems), MessageType.Error);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Roll 10 times (preview)"))
                PreviewRolls(table);
        }

        static void PreviewRolls(RandomTable table)
        {
            var rng = new Rng((uint)Random.Range(1, int.MaxValue));
            var lines = new List<string>();

            for (int i = 0; i < 10; i++)
            {
                TableEntry entry = table.Roll(rng, out int rolled);
                string label = entry == null
                    ? "<no row covers this result>"
                    : (!string.IsNullOrEmpty(entry.key) ? entry.key : entry.text);
                lines.Add($"  {rolled,3}  {label}");
            }

            Debug.Log($"[{table.name}] preview rolls:\n{string.Join("\n", lines)}", table);
        }
    }
}
