using System;
using System.IO;
using SVO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityTemplateProjects
{
	public class VoxelPlacer : MonoBehaviour
	{
		public Texture2D noiseSource;
		public bool quickPlace = false;
		[Range(5, 12)]
		public int initialDepth = 5;
		public GameObject octreeObj;
		public GameObject selectionCube;
		public Material material;
		private Octree _octree;
		private RayHit? lastHit = null;
		private float depth = 5;
	
		// Start is called before the first frame update
		void Start()
		{
			_octree = new Octree();
			_octree.MaxDepth = initialDepth;

			for (int j = 0; j < noiseSource.height; ++j)
				for (int i = 0; i < noiseSource.width; ++i)
					_octree.AddColor(noiseSource.GetPixel(i, j));

			if (noiseSource.width != Mathf.Pow(2f, initialDepth) || noiseSource.height != Mathf.Pow(2f, initialDepth))
				return;

			float x = -.5f, inc = 1 / Mathf.Pow(2f, initialDepth);
			for (int i = 0; i < Mathf.Pow(2f, initialDepth); ++i)
			{
				float z = -.5f;
				for (int j = 0; j < Mathf.Pow(2f, initialDepth); ++j)
				{
					Color pixel = noiseSource.GetPixel(i, j);
					float mean_color = (pixel.r + pixel.g + pixel.b) / 3;
					float height = (mean_color - 0.5f) / 2.0f;
					for (var y = -.25f; y <= height; y += inc)
						_octree.SetVoxel(new Vector3(x, y, z), initialDepth, noiseSource.GetPixel(i, j), new int[0]);
					z += inc;
				}
				x += inc;
			}

			material.mainTexture = _octree.Apply();

			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.T))
			{
				_octree.Rebuild();
				octreeObj.GetComponent<Renderer>().material.mainTexture = _octree.Apply();
			}

			if (Input.GetKeyDown(KeyCode.G))
				quickPlace = !quickPlace;
			
			if (Input.GetKeyDown(KeyCode.Q))
				depth++;
			if (Input.GetKeyDown(KeyCode.E))
				depth--;
			depth = Mathf.Clamp(depth, 0, 21);

			var depthInt = Mathf.RoundToInt(depth);
			var size = 1f / Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(Mathf.Exp(Mathf.Log(2) * depth)));
			
			var ray = new Ray(transform.position, transform.forward);
			if (_octree.CastRay(ray, octreeObj.transform, out var hit))
			{
				var p = (hit.objPos + Vector3.one * 0.5f + hit.faceNormal * 4.76837158203125e-7f) / size;
				p = new Vector3(Mathf.Floor(p.x) + 1, Mathf.Floor(p.y), Mathf.Floor(p.z)) * size - Vector3.one * 0.5f;
				selectionCube.transform.position =
					octreeObj.transform.localToWorldMatrix * new Vector4(p.x, p.y, p.z, 1);
				selectionCube.transform.localScale = Vector3.one * (size * 256f);
				lastHit = hit;

				if (quickPlace ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0))
				{
					_octree.SetVoxel(hit.objPos + hit.faceNormal * 4.76837158203125e-7f, depthInt, Color.white, new int[0]);
					octreeObj.GetComponent<Renderer>().material.mainTexture = _octree.Apply();
				}

				if (quickPlace ? Input.GetMouseButton(2) : Input.GetMouseButtonDown(2))
				{
					_octree.SetVoxel(hit.objPos - hit.faceNormal * 4.76837158203125e-7f, depthInt, Color.clear, new int[0]);
					octreeObj.GetComponent<Renderer>().material.mainTexture = _octree.Apply();
				}
			}
			else
			{
				lastHit = null;
			}
		}

		private void OnDrawGizmos()
		{
			if (lastHit != null)
			{
				Gizmos.DrawRay(lastHit.Value.worldPos, lastHit.Value.faceNormal);
				var pos = lastHit.Value.voxelObjPos + Vector3.one * lastHit.Value.voxelObjSize / 2;
				Gizmos.DrawWireCube(octreeObj.transform.localToWorldMatrix * new Vector4(pos.x, pos.y, pos.z, 1), 
					octreeObj.transform.lossyScale * lastHit.Value.voxelObjSize);
			}
		}
	}
}