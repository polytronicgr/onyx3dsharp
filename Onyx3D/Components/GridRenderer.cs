﻿using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Onyx3D
{
	public class GridRenderer : MeshRenderer
	{

		public void GenerateGridMesh(int cellsX, int cellsY, float cellW, float cellH, Vector3 col)
		{
			Mesh = new Mesh();

			Vector3 offset = new Vector3(-cellsX * cellW / 2.0f, 0, -cellsY * cellH / 2.0f);
			for (int i = 0; i <= cellsX; i++)
			{
				Mesh.Vertices.Add(new Vertex(new Vector3(i * cellW, 0, 0) + offset, col));
				Mesh.Vertices.Add(new Vertex(new Vector3(i * cellW, 0, cellsY * cellH) + offset, col));
			}

			for (int i = 0; i <= cellsY; i++)
			{
				Mesh.Vertices.Add(new Vertex(new Vector3(0, 0, i * cellH) + offset, col));
				Mesh.Vertices.Add(new Vertex(new Vector3(cellsX * cellW, 0, i * cellH) + offset, col));
			}

			Mesh.GenerateVAO();
		}

		public override void Render()
		{
			SetUpMaterial();
			SetUpMVP(Material.Shader.Program);

			GL.BindVertexArray(Mesh.VertexArrayObject);
			GL.DrawArrays(PrimitiveType.Lines, 0, Mesh.Vertices.Count);
			GL.BindVertexArray(0);

			GL.UseProgram(0);
		}

	}
}
