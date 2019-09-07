using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoidsSimulator))]
public class BoidsSimulatorInspector : Editor {
	public override void OnInspectorGUI () {
		DrawDefaultInspector();

		var script = (BoidsSimulator)target;
		if (GUILayout.Button("Reset Boids")) {
			script.ResetBoids();
		}
	}
}
