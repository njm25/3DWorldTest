using Godot;
using Godot.Collections;
using static Godot.Mesh;

[Tool]
public partial class TerrainMesh : MeshInstance3D
{
	[Export]
	public Vector2I GridSize = new Vector2I(64, 64);

	[Export]
	public int Subdivisions = 63;

	[Export]
	public int Variation = 16;

	[Export]
	public float EdgeFlattenRadius = 8f;

	[Export]
	public float EdgeFlattenPower = 2f;

	[Export]
	public float EdgeFlattenTargetHeight = 0f;

	[Export]
	public bool EnableSmoothing = true;

	[Export]
	public int SmoothingIterations = 2;

	[Export]
	public float SmoothingStrength = 0.5f;

	[Export]
	public FastNoiseLite noise = new FastNoiseLite();

	[ExportToolButton("Generate Mesh")]
	public Callable GenerateMeshButton => Callable.From(LoadMesh);
	[ExportToolButton("Random Seed")]
	public Callable RandomSeedButton => Callable.From(() => { noise.SetSeed((int) GD.Randi()); });
	
	public override void _Ready()
	{
		LoadMesh();
	}

	private void LoadMesh()
	{
		PlaneMesh mesh = new PlaneMesh();
		mesh.Size = new Vector2(GridSize.X, GridSize.Y);
		mesh.SubdivideDepth = Subdivisions;
		mesh.SubdivideWidth = Subdivisions;

		SurfaceTool surfaceTool = new SurfaceTool();
		surfaceTool.CreateFrom(mesh, 0);
		Array arr = surfaceTool.CommitToArrays();
		Vector3[] vertices = (Vector3[])arr[(int) ArrayType.Vertex];

		float halfX = GridSize.X * 0.5f;
		float halfZ = GridSize.Y * 0.5f;
		float maxRadius = Mathf.Min(halfX, halfZ);
		float maxEdgeRadius = Mathf.Max(0.0001f, EdgeFlattenRadius);

		for (int i = 0; i < vertices.Length; i++)
		{
			Vector3 v = vertices[i];
			float height = noise.GetNoise2D(v.X, v.Z) * Variation;

			// Calculate distance from center for circular falloff
			float distFromCenter = Mathf.Sqrt(v.X * v.X + v.Z * v.Z);
			float distFromEdge = maxRadius - distFromCenter;

			float t = Mathf.Clamp(distFromEdge / maxEdgeRadius, 0f, 1f);
			float edgeFactor = Mathf.Pow(t, EdgeFlattenPower);

			v.Y = Mathf.Lerp(EdgeFlattenTargetHeight, height, edgeFactor);
			vertices[i] = v;
		}

		// Apply smoothing if enabled
		if (EnableSmoothing)
		{
			SmoothVertices(vertices);
		}

		arr[(int) ArrayType.Vertex] = vertices;

		ArrayMesh arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(PrimitiveType.Triangles, arr);

		surfaceTool.CreateFrom(arrayMesh, 0);
		surfaceTool.GenerateNormals();

		Mesh = surfaceTool.Commit();

	}

	private void SmoothVertices(Vector3[] vertices)
	{
		int width = Subdivisions + 1;
		int height = Subdivisions + 1;
		
		for (int iteration = 0; iteration < SmoothingIterations; iteration++)
		{
			Vector3[] smoothedVertices = new Vector3[vertices.Length];
			
			for (int i = 0; i < vertices.Length; i++)
			{
				int x = i % width;
				int z = i / width;
				
				float totalHeight = 0f;
				int count = 0;
				
				// Sample current vertex and neighbors
				for (int dx = -1; dx <= 1; dx++)
				{
					for (int dz = -1; dz <= 1; dz++)
					{
						int nx = x + dx;
						int nz = z + dz;
						
						if (nx >= 0 && nx < width && nz >= 0 && nz < height)
						{
							int neighborIndex = nz * width + nx;
							totalHeight += vertices[neighborIndex].Y;
							count++;
						}
					}
				}
				
				float averageHeight = totalHeight / count;
				smoothedVertices[i] = vertices[i];
				smoothedVertices[i].Y = Mathf.Lerp(vertices[i].Y, averageHeight, SmoothingStrength);
			}
			
			// Copy smoothed values back
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i] = smoothedVertices[i];
			}
		}
	}
}
