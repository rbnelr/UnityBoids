using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Collections.Generic;

public class BoidsSimulator : MonoBehaviour {
	
	public float2 Size = float2(100,100);
	public int BoidsCount = 100;
	public float SenseRadius = 25f;
	public float SeperationRadius = 5f;

	public float SpeedTarget = 50;

	public float WallStrength = 5f;
	public float ControlSpeedStrength = 1f;
	public float AlignmentStrength = 1f;
	public float SeperationStrength = 10f;
	public float CohesionStrength = 2f;

	public Mesh Mesh;
	public Material MeshMaterial;

	public Camera Cam;
	public bool TrackBoid = true;

	Unity.Mathematics.Random rand = new Unity.Mathematics.Random(1);

	struct Boid {
		public float2 pos;
		public float2 vel;

		public float2 speed;
		public float2 align;
		public float2 sep;
		public float2 coh;
	};

	List<Boid> boids = new List<Boid>();

	Boid spawn_boid () {
		return new Boid {
			pos = rand.NextFloat2() * Size,
			vel = rand.NextFloat2Direction() * rand.NextFloat2() * 2 * SpeedTarget,
		};
	}

	private void Update () {
		if (BoidsCount < boids.Count) {
			boids.RemoveRange(BoidsCount, boids.Count - BoidsCount);
		}
		while (BoidsCount > boids.Count) {
			boids.Add( spawn_boid() );
		}

		for (int i=0; i<boids.Count; ++i) {
			var boid = boids[i];
			
			float2 avg_vel = 0.0f;
			int avg_vel_count = 0;

			float2 avg_seperation = 0;
			int avg_seperation_count = 0;

			float2 avg_cohesion = 0;
			int avg_cohesion_count = 0;

			for (int j=0; j<boids.Count; ++j) {
				if (i == j) continue;
				var other = boids[j];
				
				float2 offs = other.pos - boid.pos;
				float dist = length(offs);

				if (dist <= SenseRadius) {
					avg_vel += other.vel;
					avg_vel_count++;
				}
				if (dist <= SeperationRadius) {
					avg_seperation += offs;
					avg_seperation_count++;
				}
				if (dist <= SenseRadius) {
					avg_cohesion += offs;
					avg_cohesion_count++;
				}
			}

			if (avg_vel_count > 0)			avg_vel /= avg_vel_count;
			if (avg_seperation_count > 0)	avg_seperation /= avg_seperation_count;
			if (avg_cohesion_count > 0)		avg_cohesion /= avg_cohesion_count;

			float2 speed = length(boid.vel);
			float2 forward = normalizesafe(boid.vel);

			float2 wallVec = clamp(boid.pos, 0, Size) - boid.pos;
			wallVec *= length(wallVec);

			float2 accel = 0f;

			float2 wall = wallVec * WallStrength;
			boid.speed = forward * (SpeedTarget - speed) * ControlSpeedStrength;
			boid.align = (normalizesafe(avg_vel) * SpeedTarget - boid.vel) * AlignmentStrength;
			boid.sep =  -avg_seperation * SeperationStrength;
			boid.coh =  avg_cohesion * CohesionStrength;

			accel += wall;
			accel += boid.speed;
			accel += boid.align;
			accel += boid.sep;
			accel += boid.coh;

			boid.vel += accel * Time.deltaTime;
			boid.pos += boid.vel * Time.deltaTime;

			boids[i] = boid;

			var mat = Matrix4x4.TRS(float3(boid.pos, 0), Quaternion.LookRotation(float3(boid.vel, 0), Vector3.up), float3(1));

			Graphics.DrawMesh(Mesh, mat, MeshMaterial, 0);
		}

		if (TrackBoid && boids.Count > 0)
			Cam.transform.position = float3(boids[0].pos, Cam.transform.position.z);
	}

	private void OnDrawGizmos () {
		if (boids.Count <= 0) return;

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(float3(boids[0].pos, 0), SenseRadius);
		
		Gizmos.color = Color.yellow;
			Gizmos.DrawRay(float3(boids[0].pos, -1), float3(boids[0].speed, 0) / 3);
		Gizmos.color = Color.green;
			Gizmos.DrawRay(float3(boids[0].pos, -1), float3(boids[0].align, 0) / 3);
		Gizmos.color = Color.blue;
			Gizmos.DrawRay(float3(boids[0].pos, -1), float3(boids[0].sep, 0) / 3);
		Gizmos.color = Color.black;
			Gizmos.DrawRay(float3(boids[0].pos, -1), float3(boids[0].coh, 0) / 3);
		
		for (int j=0; j<boids.Count; ++j) {
			if (0 == j) continue;

			float dist = distance(boids[j].pos, boids[0].pos);
			if (dist <= SenseRadius) {
				Gizmos.color = dist <= SeperationRadius ? Color.blue : Color.red;
				Gizmos.DrawWireSphere(float3(boids[j].pos, 0), 1f);
			}
		}

		for (int i=0; i<boids.Count; ++i) {
		Gizmos.color = Color.red;
			Gizmos.DrawRay(float3(boids[i].pos, 0), float3(boids[i].vel, 0) / 5);
			
		}
	}
}
