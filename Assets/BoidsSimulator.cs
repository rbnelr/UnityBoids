using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Collections.Generic;

public class BoidsSimulator : MonoBehaviour {

	public float2 WorldRect = float2(100,100);
	
	public BoidKind[] Kinds;
	
	public Camera Cam;
	public int TrackBoid = 0;

	Unity.Mathematics.Random rand = new Unity.Mathematics.Random(1);

	private void Start () {
		foreach (var k in Kinds)
			k.Start();
	}

	public void ResetBoids () {
		foreach (var k in Kinds)
			k.ResetBoids();
	}

	private void Update () {
		for (int i=0; i<Kinds.Length; ++i)
			Kinds[i].Update(ref rand, WorldRect, i == TrackBoid, Cam);
	}

	private void OnDrawGizmos () {
		// Area outlines
		Gizmos.color = Color.grey;
		Gizmos.DrawWireCube(float3(WorldRect / 2, 0), float3(WorldRect, 1));

		foreach (var k in Kinds)
			k.OnDrawGizmos();
	}
}
