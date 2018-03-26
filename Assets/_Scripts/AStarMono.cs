using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AStarMono : MonoBehaviour
{
	[Header("Setup")]
	[SerializeField] private int _width = 10;
	[SerializeField] private int _height = 10;
	[SerializeField, Range(0, 1f), Tooltip("0, no obstacle, 1, obstacles in each cases")] private float _fillRatio = 0.3f;
	[SerializeField] private Vector2 _start;
	[SerializeField] private Vector2 _end;

	[Header("Links")]
	[SerializeField] private Transform _worldSpawn;
	[SerializeField] private GameObject _cubePrefab;

	[Header("Links/Materials")]
	[SerializeField] private Material _cubeGrey;
	[SerializeField] private Material _cubeRed;
	[SerializeField] private Material _cubeGreen;
	[SerializeField] private Material _cubeBlue;

	private int[,] _map;
	private Material[,] _mapCubesMaterials; // We use this array to colorize the possible path while the algorithm is running
	private Vector3 _cubeDimensions;
	private Vector3 _centerTranslation;

	private void Start()
	{
		WorldBuilder();
	}

	#region World Builder

	private void WorldBuilder()
	{
		if (_width <= 2 || _height <= 2)
		{
			Debug.LogError("Width and Heigh must be at least 2");
			return;
		}

		Mesh cubeMesh = _cubePrefab.GetComponent<MeshFilter>().sharedMesh;
		if (cubeMesh == null)
		{
			Debug.LogError("Your Cube prefabs must have a mesh filter and mesh");
			return;
		}

		// Initialized to default value = 0;
		_map = new int[_height, _width];
		_mapCubesMaterials = new Material[_height, _width];

		if (_start.x < 0) _start.x = 0;
		if (_start.y < 0) _start.y = 0;
		if (_start.x >= _width) _start.x = _width - 1;
		if (_start.y >= _height) _start.y = _height - 1;

		if (_end.x < 0) _end.x = 0;
		if (_end.y < 0) _end.y = 0;
		if (_end.x >= _width) _end.x = _width - 1;
		if (_end.y >= _height) _end.y = _height - 1;

		_cubeDimensions = cubeMesh.bounds.size;
		_centerTranslation = new Vector3((_width * _cubeDimensions.x) / 2, 0, (_height * _cubeDimensions.z) / 2);
		// Create Obstacles
		for (int y = 0; y < _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				GameObject temp = Instantiate(_cubePrefab, _worldSpawn);
				temp.transform.position = new Vector3(x * _cubeDimensions.x, -_cubeDimensions.y + _cubeDimensions.y / 2, y * _cubeDimensions.z) - _centerTranslation;
				_mapCubesMaterials[y, x] = temp.GetComponent<MeshRenderer>().material;

				if ((Math.Abs(x - _start.x) < 0.05f && Math.Abs(y - _start.y) < 0.05f) || (Math.Abs(x - _end.x) < 0.05f && Math.Abs(y - _end.y) < 0.05f)) continue;

				if (Random.Range(0.0f, 1.0f) >= _fillRatio) continue;

				_map[y, x] = 1;

				temp = Instantiate(_cubePrefab, _worldSpawn);
				temp.GetComponent<MeshRenderer>().material = _cubeGrey;
				temp.name = "obstacle";
				temp.transform.position = new Vector3(x * _cubeDimensions.x, _cubeDimensions.y / 2, y * _cubeDimensions.z) - _centerTranslation;
			}
		}

		// Create border Walls
		for (int y = 0; y < _height + 2; y++)
		{
			GameObject temp = Instantiate(_cubePrefab, _worldSpawn);
			temp.GetComponent<MeshRenderer>().material = _cubeGrey;
			temp.name = "border";
			temp.transform.position = new Vector3(-_cubeDimensions.x, _cubeDimensions.y / 2, (y - 1) * _cubeDimensions.z) - _centerTranslation;
			temp = Instantiate(_cubePrefab, _worldSpawn);
			temp.GetComponent<MeshRenderer>().material = _cubeGrey;
			temp.name = "border";
			temp.transform.position = new Vector3(_width * _cubeDimensions.x, _cubeDimensions.y / 2, (y - 1) * _cubeDimensions.z) - _centerTranslation;
		}
		for (int x = 0; x < _width; x++)
		{
			GameObject temp = Instantiate(_cubePrefab, _worldSpawn);
			temp.GetComponent<MeshRenderer>().material = _cubeGrey;
			temp.name = "border";
			temp.transform.position = new Vector3(x * _cubeDimensions.x, _cubeDimensions.y / 2, -_cubeDimensions.z) - _centerTranslation;
			temp = Instantiate(_cubePrefab, _worldSpawn);
			temp.GetComponent<MeshRenderer>().material = _cubeGrey;
			temp.name = "border";
			temp.transform.position = new Vector3(x * _cubeDimensions.x, _cubeDimensions.y / 2, _height * _cubeDimensions.z) - _centerTranslation;
		}
	}

	#endregion

}
