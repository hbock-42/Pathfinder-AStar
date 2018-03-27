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
	[SerializeField, Tooltip("Can we move diagonaly")] private bool _canMoveDiago;
	[SerializeField, Tooltip("Show the algorithm working live, this slows a lot")] private bool _liveShow = true;

	[Header("Links")]
	[SerializeField] private Transform _worldSpawn;
	[SerializeField] private GameObject _cubePrefab;
	[SerializeField] private GameObject _targetGo;

	[Header("Links/Materials")]
	[SerializeField] private Material _cubeGrey;
	[SerializeField] private Material _cubeRed;
	[SerializeField] private Material _cubeGreen;
	[SerializeField] private Material _cubeBlue;

	private int[,] _map;
	private Material[,] _mapCubesMaterials; // We use this array to colorize the possible path while the algorithm is running
	private GameObject[,] _mapCubes; // We use this array to colorize the possible path while the algorithm is running
	private Vector3 _cubeDimensions;
	private Vector3 _centerTranslation;
	private Dictionary<string, Vector2> _hashVectorCorrespondance = new Dictionary<string, Vector2>();
	private List<Vector2> _currentPath;

	private void Start()
	{
		WorldBuilder();
		StartCoroutine(AStar());
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
		_mapCubes = new GameObject[_height, _width];

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
				_mapCubes[y, x] = temp;

				if (x == Mathf.RoundToInt(_end.x) && y == Mathf.RoundToInt(_end.y))
				{
					temp = Instantiate(_targetGo, _worldSpawn);
					temp.name = "Target";
					temp.transform.position = _mapCubes[y, x].transform.position;
					continue;
				}
				if ((Math.Abs(x - _start.x) < 0.05f && Math.Abs(y - _start.y) < 0.05f)) continue;

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


	#region A* Algorithme

	// https://en.wikipedia.org/wiki/A*_search_algorithm
	IEnumerator AStar()
	{
		bool found = false;
		List<Vector2> closedSet = new List<Vector2>();
		List<Vector2> openSet = new List<Vector2> {_start};
		List<Vector2> neighbors = new List<Vector2>();

		Dictionary<string, string> cameFrom = new Dictionary<string, string>();
		Dictionary<string, float> gScore = new Dictionary<string, float>();
		gScore[HashVector(_start)] = 0;
		Dictionary<string, float> fScore = new Dictionary<string, float>();
		fScore[HashVector(_start)] = HeuristicCostEstimation(_start, _end);
		_hashVectorCorrespondance[HashVector(_start)] = _start;

		while (openSet.Count > 0)
		{
			Vector2 current = GetNodeWithBestFScore(openSet, fScore);

			if (current.EqualRounded(_end))
			{
				Debug.Log("Path Found");
				ColorCubes(closedSet, openSet, cameFrom, current);
				found = true;
				break;
			}

			openSet.Remove(current);
			closedSet.Add(current);

			GetNeighbors(ref neighbors, current);
			for (int i = 0; i < neighbors.Count; i++)
			{
				if (IsInList(ref closedSet, neighbors[i])) continue;
				if (!IsInList(ref openSet, neighbors[i])) openSet.Add(neighbors[i]);

				if (!gScore.ContainsKey(HashVector(neighbors[i])))
				{
					gScore[HashVector(neighbors[i])] = float.PositiveInfinity;
					_hashVectorCorrespondance[HashVector(neighbors[i])] = neighbors[i];
				}
				float tentativeGScore = gScore[HashVector(current)] + VectorDistance(current, neighbors[i]);
				if (tentativeGScore >= gScore[HashVector(neighbors[i])]) continue;

				cameFrom[HashVector(neighbors[i])] = HashVector(current);
				gScore[HashVector(neighbors[i])] = tentativeGScore;
				fScore[HashVector(neighbors[i])] = gScore[HashVector(neighbors[i])] + HeuristicCostEstimation(neighbors[i], _end);
			}

			ColorCubes(closedSet, openSet, cameFrom, current);
			if (!_liveShow) continue;
			yield return new WaitForEndOfFrame();
		}

		if (!found)
		{
			Debug.Log("Nothing found");
		}

		yield return null;
	}


	/// <summary>
	/// Create a Hash from a Vector2
	/// x and y are cast into int, use this as if you were working with int Vectors
	/// </summary>
	/// <returns>A hash usable with dictionary</returns>
	private string HashVector(Vector2 pVector)
	{
		return Mathf.RoundToInt(pVector.x) + "-" + Mathf.RoundToInt(pVector.y);
	}

	/// <summary>
	/// Calcul the estimated cost to go from pFrom to pTarget
	/// </summary>
	/// <returns>Return the estimated cost</returns>
	private float HeuristicCostEstimation(Vector2 pFrom, Vector2 pTarget)
	{
		// This seems stupid but I want to keep the choice to change my heristic cost estimation later
		return VectorDistance(pFrom, pTarget);
	}

	/// <summary>
	/// World Distance between pFrom and pTarget
	/// We take in account the _cubeDimensions calculated earlier
	/// </summary>
	/// <returns>The distance between pFrom and pTarget</returns>
	private float VectorDistance(Vector2 pFrom, Vector2 pTarget)
	{
		Vector2 diff = pTarget - pFrom;
		diff.x *= _cubeDimensions.x;
		diff.y *= _cubeDimensions.z;
		return diff.magnitude;
	}

	/// <summary>
	/// Return the vector with the best fScore
	/// </summary>
	/// <param name="pOpenSet">List of vector we are comparing the fscores</param>
	/// <param name="pFScore">fscore dictionnary</param>
	/// <returns></returns>
	private Vector2 GetNodeWithBestFScore(List<Vector2> pOpenSet, Dictionary<string, float> pFScore)
	{
		int bestId = 0;
		for (int i = 0; i < pOpenSet.Count; i++)
		{
			if (pFScore[HashVector(pOpenSet[i])] < pFScore[HashVector(pOpenSet[bestId])])
			{
				bestId = i;
			}
		}

		return pOpenSet[bestId];
	}

	/// <summary>
	/// Get the neighbors of a vector2 (pCurrent)
	/// </summary>
	/// <param name="pNeighbors">List of neighbors, we fill it</param>
	/// <param name="pCurrent">The Vector we are searching the neighbors</param>
	private void GetNeighbors(ref List<Vector2> pNeighbors, Vector2 pCurrent)
	{
		pNeighbors.Clear();

		bool left = false, right = false, up = false, down = false;

		Vector2 tmp;

		// Check left
		tmp = pCurrent - Vector2.right;
		if (Mathf.RoundToInt(tmp.x) >= 0 && _map[Mathf.RoundToInt(tmp.y), Mathf.RoundToInt(tmp.x)] == 0)
		{
			left = true;
			pNeighbors.Add(tmp);
		}
		// Check right
		tmp = pCurrent + Vector2.right;
		if (Mathf.RoundToInt(tmp.x) < _width && _map[Mathf.RoundToInt(tmp.y), Mathf.RoundToInt(tmp.x)] == 0)
		{
			right = true;
			pNeighbors.Add(tmp);
		}
		// Check up
		tmp = pCurrent - Vector2.up;
		if (Mathf.RoundToInt(tmp.y) >= 0 && _map[Mathf.RoundToInt(tmp.y), Mathf.RoundToInt(tmp.x)] == 0)
		{
			up = true;
			pNeighbors.Add(tmp);
		}
		// Check down
		tmp = pCurrent + Vector2.up;
		if (Mathf.RoundToInt(tmp.y) < _height && _map[Mathf.RoundToInt(tmp.y), Mathf.RoundToInt(tmp.x)] == 0)
		{
			down = true;
			pNeighbors.Add(tmp);
		}

		if (!_canMoveDiago) return;

		// Check left up
		tmp = pCurrent - Vector2.right - Vector2.up;
		if (left && up && Mathf.RoundToInt(tmp.x) >= 0 && Mathf.RoundToInt(tmp.y) >= 0 && _map[Mathf.RoundToInt(tmp.y), Mathf.RoundToInt(tmp.x)] == 0) pNeighbors.Add(tmp);
		// Check right up
		tmp = pCurrent + Vector2.right - Vector2.up;
		if (right && up && Mathf.RoundToInt(tmp.x) < _width && Mathf.RoundToInt(tmp.y) >= 0 && _map[Mathf.RoundToInt(tmp.y), Mathf.RoundToInt(tmp.x)] == 0) pNeighbors.Add(tmp);
		// Check left down
		tmp = pCurrent - Vector2.right + Vector2.up;
		if (left && down && Mathf.RoundToInt(tmp.x) >= 0 && Mathf.RoundToInt(tmp.y) < _height && _map[Mathf.RoundToInt(tmp.y), Mathf.RoundToInt(tmp.x)] == 0) pNeighbors.Add(tmp);
		// Check right down
		tmp = pCurrent + Vector2.right + Vector2.up;
		if (right && down && Mathf.RoundToInt(tmp.x) < _width && Mathf.RoundToInt(tmp.y) < _height && _map[Mathf.RoundToInt(tmp.y), Mathf.RoundToInt(tmp.x)] == 0) pNeighbors.Add(tmp);
	}

	/// <summary>
	/// </summary>

	/// <summary>
	/// Function to determine if pToCheck is in pList
	/// </summary>
	/// <param name="pList">List to search into</param>
	/// <param name="pToCheck">Vector to search for</param>
	/// <returns>True if pToCheck is found, else false</returns>
	private bool IsInList(ref List<Vector2> pList, Vector2 pToCheck)
	{
		foreach (Vector2 v1 in pList)
		{
			if (v1.EqualRounded(pToCheck)) return true;
		}

		return false;
	}


	private List<Vector2> GetPath(Dictionary<string, string> pCameFrom, Vector2 pCurrent)
	{
		List<Vector2> lPath = new List<Vector2>(){ pCurrent };
		string hash = HashVector(pCurrent);
		while (pCameFrom.ContainsKey(hash))
		{
			hash = pCameFrom[hash];
			lPath.Insert(0, _hashVectorCorrespondance[hash]);
		}

		return lPath;
	}

	#endregion

	#region Display

	/// <summary>
	/// Color the cube to show the state they are currently in
	/// </summary>
	private void ColorCubes(List<Vector2> pClosedSet, List<Vector2> pOpenSet, Dictionary<string, string> pCameFrom, Vector2 pCurrent)
	{
		foreach (Vector2 v2 in pClosedSet)
		{
			_mapCubesMaterials[Mathf.RoundToInt(v2.y), Mathf.RoundToInt(v2.x)] = _cubeBlue;

			_mapCubes[Mathf.RoundToInt(v2.y), Mathf.RoundToInt(v2.x)].GetComponent<MeshRenderer>().material = _cubeBlue;
				
		}

		foreach (Vector2 v2 in pOpenSet)
		{
			_mapCubesMaterials[Mathf.RoundToInt(v2.y), Mathf.RoundToInt(v2.x)] = _cubeRed;

			 _mapCubes[Mathf.RoundToInt(v2.y), Mathf.RoundToInt(v2.x)].GetComponent<MeshRenderer>().material = _cubeRed;
		}

		_currentPath = GetPath(pCameFrom, pCurrent);
		foreach (Vector2 v2 in _currentPath)
		{
			_mapCubesMaterials[Mathf.RoundToInt(v2.y), Mathf.RoundToInt(v2.x)] = _cubeGreen;

			_mapCubes[Mathf.RoundToInt(v2.y), Mathf.RoundToInt(v2.x)].GetComponent<MeshRenderer>().material = _cubeGreen;
		}
	}

	#endregion
}

public static class MyExtensions
{
	/// <summary>
	/// Round the x and y component of the vectors pV1 and pV2 then make an equality comparision
	/// </summary>
	public static bool EqualRounded(this Vector2 pV1, Vector2 pV2)
	{
		return Mathf.RoundToInt(pV1.x) == Mathf.RoundToInt(pV2.x) && Mathf.RoundToInt(pV1.y) == Mathf.RoundToInt(pV2.y);
	}
}