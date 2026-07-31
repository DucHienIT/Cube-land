using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CubeBlaster.EditorTools
{
    /// The sculpture is thousands of packed cubes, so the default inspector can only show the
    /// level's knobs and none of its consequences. This adds the two things you actually need
    /// when touching a level by hand: what is in it, and whether it is still winnable.
    ///
    /// The solvability check is not decoration. Guns are colour-locked and there is no surplus
    /// ammo, so editing a single bank number by one makes the level impossible to finish, with
    /// no symptom until you play it to the end.
    [CustomEditor(typeof(LevelAsset))]
    public class LevelAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var asset = (LevelAsset)target;
            var cubes = asset.CountVoxelsPerColor();
            var ammo = asset.CountAmmoPerColor();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sculpture", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Cubes", asset.VoxelCount.ToString("N0"));
            EditorGUILayout.LabelField("Bank blocks", asset.bank != null ? asset.bank.Length.ToString() : "0");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cubes vs ammo, per colour", EditorStyles.boldLabel);
            foreach (int color in SortedColors(cubes, ammo))
            {
                cubes.TryGetValue(color, out int cubeCount);
                ammo.TryGetValue(color, out int ammoCount);
                EditorGUILayout.LabelField(
                    "slot " + color,
                    string.Format("{0:N0} cubes / {1:N0} ammo{2}", cubeCount, ammoCount,
                        cubeCount == ammoCount ? "" : "   <-- mismatch"));
            }

            EditorGUILayout.Space();
            var issues = asset.FindBankIssues();
            if (issues.Count == 0)
                EditorGUILayout.HelpBox("Solvable: every colour has exactly one dart per cube.",
                    MessageType.Info);
            else
                EditorGUILayout.HelpBox(
                    "This level cannot be finished:\n- " + string.Join("\n- ", issues),
                    MessageType.Error);

            EditorGUILayout.HelpBox(
                "Cube positions are generated — run `python Tools/gen_levels.py` to rebuild every " +
                "level. Editing the bank here will not regenerate to match.", MessageType.None);
        }

        static List<int> SortedColors(Dictionary<int, int> cubes, Dictionary<int, int> ammo)
        {
            var colors = new List<int>(cubes.Keys);
            foreach (int color in ammo.Keys)
                if (!cubes.ContainsKey(color)) colors.Add(color);
            colors.Sort();
            return colors;
        }
    }
}
