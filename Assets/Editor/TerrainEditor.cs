using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(TerrainSettings))]
public class TerrainEditor : Editor {

    public override void OnInspectorGUI() {
      var terrain =  (TerrainSettings)target;

      EditorGUI.BeginChangeCheck();

      base.OnInspectorGUI();

      if (EditorGUI.EndChangeCheck()) {
        terrain.Recreate();
      }



    }

}
#endif
