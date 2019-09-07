using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Collections.Generic;
using UnityEngine.Profiling;

public class BoidsSimulator : MonoBehaviour {
	
	// http://www.cs.toronto.edu/~dt/siggraph97-course/cwr87/

	public float3 Size = float3(100,100,100);
	public int BoidsCount = 100;
	
	public float SenseRadius = 25;
	public float AvoidRadius = 5;
	public float SenseAngle = 360 - 70;

	public float SpeedTarget = 25;
	public float BoidAccel = 25;
	public float BoidManeuverBudget = 50;
	
	[Range(0, 1)]
	public float AvoidStrength = 0.8f;
	[Range(0, 1)]
	public float AlignStrength = 0.5f;
	[Range(0, 1)]
	public float CenterStrength = 0.3f;

	float Drag (float speed) => (BoidAccel / (SpeedTarget * SpeedTarget)) * (speed * speed); // Drag=BoidAccel at speed=SpeedTarget

	//public float AlignmentStrength = 1f;
	//public float SeperationStrength = 10f;
	//public float CohesionStrength = 2f;

	public float BoidSize = 1;
	public Mesh BoidMesh;
	public Material BoidMaterial;
	public Gradient BoidColors;

	public Camera Cam;
	public bool TrackBoid = true;

	Unity.Mathematics.Random rand = new Unity.Mathematics.Random(1);

	struct Boid {
		public float3 pos;
		public float3 vel;
		public float3 forward;
		
		public float3 avoid;
		public float3 align;
		public float3 center;

		public Color color;
	};

	List<Boid> boids = new List<Boid>();

	private void Start () {
		proceduralMesh = new Mesh();
		proceduralMesh.MarkDynamic();
	}

	Boid spawn_boid () {
		return new Boid {
			pos = rand.NextFloat3() * Size,
			vel = rand.NextFloat3Direction() * rand.NextFloat() * 0.2f * SpeedTarget,
			forward = float3(0, 0, 1),

			color = BoidColors.Evaluate(rand.NextFloat()),
		};
	}

	void SpawnBoids () {
		if (BoidsCount < boids.Count) {
			boids.RemoveRange(BoidsCount, boids.Count - BoidsCount);
		}
		while (BoidsCount > boids.Count) {
			boids.Add( spawn_boid() );
		}	
	}
	public void ResetBoids () {
		if (boids.Count > 0) {
			boids.RemoveRange(0, boids.Count);
		}
	}

	void UpdateBoids () {
		SpawnBoids();

		for (int i=0; i<boids.Count; ++i) {
			var boid = boids[i];
			
			float speed = length(boid.vel);

			if (speed != 0)
				boid.forward = boid.vel / speed;

			float3 accel = 0;

			accel +=  boid.forward * BoidAccel; // always accelerate forward
			accel += -boid.forward * Drag(speed); // drag limits velocity

			accel += BoidLogic(i, ref boid, speed);

			boid.vel += accel * Time.deltaTime;
			boid.pos += boid.vel * Time.deltaTime;

			boid.pos = boid.pos % Size;
			boid.pos = select(boid.pos, boid.pos + Size, boid.pos < 0);

			DrawBoid(boid);

			boids[i] = boid;
		}
	}

	float3 BoidLogic (int i, ref Boid boid, float speed) {
		
		float SenseCosTheta = math.cos(math.radians(SenseAngle));

		float radius = SenseRadius * BoidSize;

		float3 avoidAvg = 0;
		//float avoidCount = 0;

		float3 alignAvg = 0;
		float alignCount = 0;

		float3 centerAvg = 0;
		float centerCount = 0;
		
	Profiler.BeginSample("BoidLogic inner loop");

		for (int j=0; j<boids.Count; ++j) {
			if (i == j) continue;
			var other = boids[j];
			
			float3 offs = other.pos - boid.pos;
			float dist = length(offs);
			float3 dir = dist != 0 ? offs / dist : 0;

			if (dist > radius || dot(boid.forward, dir) < SenseCosTheta) continue;

			float sensitivity = (1 - dist / radius); // inverse square falloff of sensitivity
			sensitivity *= sensitivity;

			if (dist <= AvoidRadius * BoidSize) {
				float d = dist;
				d /= AvoidRadius * BoidSize; // stong falloff

				float stren = 1f / (d + 0.2f) - 0.83f;

				avoidAvg += -dir * stren;
			}

			alignAvg += (other.vel - boid.vel) * sensitivity;
			alignCount += 1;
			
			centerAvg += offs * sensitivity;
			centerCount += 1;
		}

	Profiler.EndSample();

		//if (avoidCount != 0) avoidAvg /= avoidCount;
		if (alignCount != 0) alignAvg /= alignCount;
		if (centerCount != 0) centerAvg /= centerCount;

		float accelBudget = BoidManeuverBudget;

		float3 avoid = avoidAvg * AvoidStrength * BoidManeuverBudget;
		float3 align = alignAvg * AlignStrength * BoidManeuverBudget;
		float3 center =  centerAvg * CenterStrength * BoidManeuverBudget;

		float avoidMag = min(length(avoid), accelBudget);
		accelBudget -= avoidMag;

		float alignMag = min(length(align), accelBudget);
		accelBudget -= alignMag;

		float centerMag = min(length(center), accelBudget);
		accelBudget -= centerMag;

		avoid = normalizesafe(avoid) * avoidMag;
		align = normalizesafe(align) * alignMag;
		center = normalizesafe(center) * centerMag;

		boid.avoid = avoid;
		boid.align = align;
		boid.center = center;

		return avoid + align + center;
	}
	
	Mesh proceduralMesh;
	List<Vector3> vertices = new List<Vector3>();
	List<Color> colors = new List<Color>();
	List<int> triangles = new List<int>();
	
	Matrix4x4 Matrix (Boid boid) => Matrix4x4.TRS(boid.pos, Quaternion.LookRotation(boid.forward, float3(0,1,0)), float3(BoidSize));

	void DrawBoid (Boid boid) {
	Profiler.BeginSample("DrawBoid");
		
		var mat = Matrix(boid);

		int triOffs = vertices.Count;
			
		for (int k=0; k<BoidMesh.vertices.Length; ++k) {
			vertices.Add(mat.MultiplyPoint(BoidMesh.vertices[k]));
			colors.Add(boid.color);
		}

		for (int k=0; k<BoidMesh.triangles.Length; ++k) {
			triangles.Add(triOffs + BoidMesh.triangles[k]);
		}

	Profiler.EndSample();
	}

	private void Update () {
		proceduralMesh.Clear();
		vertices.Clear();
		colors.Clear();
		triangles.Clear();
		
	Profiler.BeginSample("UpdateBoids");
		UpdateBoids();
	Profiler.EndSample();

		proceduralMesh.SetVertices(vertices);
		proceduralMesh.SetColors(colors);
		proceduralMesh.SetTriangles(triangles, 0);

		Graphics.DrawMesh(proceduralMesh, new Vector3(0,0,0), Quaternion.identity, BoidMaterial, 0);

		//if (TrackBoid && boids.Count > 0)
		//	Cam.transform.position = float3(boids[0].pos, Cam.transform.position.z);
	}

	private void OnDrawGizmos () {
	Profiler.BeginSample("OnDrawGizmos");

		// Area outlines
		Gizmos.color = Color.grey;
		Gizmos.DrawWireCube(Size / 2, Size);

		// Boid details
		if (boids.Count > 0) {
			Boid boid = boids[0];

			//
			Gizmos.color = Color.white;
			Gizmos.matrix = Matrix(boid);
			Gizmos.DrawWireSphere(float3(0), SenseRadius);

			Gizmos.matrix = Matrix4x4.identity;

			Gizmos.color = Color.red;
			Gizmos.DrawRay(boid.pos, boid.avoid * 2);
			Gizmos.color = Color.yellow;
			Gizmos.DrawRay(boid.pos, boid.align * 2);
			Gizmos.color = Color.cyan;
			Gizmos.DrawRay(boid.pos, boid.center * 2);

			if (true) {
				float SenseCosTheta = math.cos(math.radians(SenseAngle));
				float radius = SenseRadius * BoidSize;

				for (int j=0; j<boids.Count; ++j) {
					if (0 == j) continue;
					var other = boids[j];

					float3 offs = other.pos - boid.pos;
					float dist = length(offs);
					float3 dir = dist != 0 ? offs / dist : 0;

					if (dist > SenseRadius * BoidSize || dot(boid.forward, dir) < SenseCosTheta) continue;
					
					Gizmos.color = dist < AvoidRadius * BoidSize ? Color.red : Color.green;
					Gizmos.DrawLine(boid.pos, other.pos);
				}
			}
		}

	Profiler.EndSample();
	}
}
