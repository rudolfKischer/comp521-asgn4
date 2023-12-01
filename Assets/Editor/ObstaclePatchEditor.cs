using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ObstaclePatch))]
public class ObstaclePatchEditor : Editor {

    public override void OnInspectorGUI() {
      var obstaclePatch =  (ObstaclePatch)target;

      EditorGUI.BeginChangeCheck();

      base.OnInspectorGUI();

      if (EditorGUI.EndChangeCheck()) {
        obstaclePatch.Recreate();
      }



    }

}
#endif
