using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BlendMatrix : MonoBehaviour
{

	[SerializeField] private float _smoothTimeOrthoToPers = 0.5f;
	[SerializeField] private float _smoothTimePersToOrtho = 0.5f;
	[SerializeField] private float _fov = 60;
	[SerializeField] private float _size = 25;
	[SerializeField] private float _near = 0.3f;
	[SerializeField] private float _far = 1000f;

	private Camera _cam;
	private Matrix4x4 _matrixOrtho;
	private Matrix4x4 _matrixPersp;
	private float _aspect;

	private float _maxDelta;
	private float _minDeltaToStop = 0.001f;
	private bool _ortho;

	private void Start()
	{
		_cam = this.GetComponent<Camera>();
		_aspect = _cam.aspect;
		_ortho = _cam.orthographic;
		_matrixOrtho = Matrix4x4.Ortho(-_size * _aspect, _size * _aspect, -_size, _size, _near, _far);
		_matrixPersp = Matrix4x4.Perspective(_fov, _cam.aspect, _near, _far);
	}


	private Matrix4x4 SmoothDampMatrix(Matrix4x4 pSrc, Matrix4x4 pDst, ref float[] pVelocity, float pSmoothTime)
	{
		_maxDelta = float.NegativeInfinity;
		float _dist;
		Matrix4x4 ret = new Matrix4x4();
		for (int i = 0; i < 16; i++)
		{
			ret[i] = Mathf.SmoothDamp(pSrc[i], pDst[i], ref pVelocity[i], pSmoothTime);
			_dist = Mathf.Abs(ret[i] - pDst[i]);
			if (_dist > _maxDelta) _maxDelta = _dist;
		}

		return ret;
	}

	private IEnumerator SmoothFromTo(Matrix4x4 pSrc, Matrix4x4 pDst, float pSmoothTime)
	{
		float[] _velocitys = new float[16];
		while (_maxDelta > _minDeltaToStop)
		{
			_cam.projectionMatrix = SmoothDampMatrix(_cam.projectionMatrix, pDst, ref _velocitys, pSmoothTime);
			Debug.Log("min:" + _maxDelta);
			yield return new WaitForEndOfFrame();;
		}

		_cam.projectionMatrix = pDst;
	}

	private void BlendToMatrix(Matrix4x4 pDst, float pSmoothTime)
	{
		StopAllCoroutines();
		_maxDelta = float.PositiveInfinity;
		StartCoroutine(SmoothFromTo(_cam.projectionMatrix, pDst, pSmoothTime));
	}

	public void SwitchMatrix()
	{
		_ortho = !_ortho;
		if (_ortho) BlendToMatrix(_matrixOrtho, _smoothTimePersToOrtho);
		else BlendToMatrix(_matrixPersp, _smoothTimeOrthoToPers);
	}
}
