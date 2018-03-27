using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Smooth;

[RequireComponent(typeof(Camera))]
public class BlendMatrix : MonoBehaviour
{
	[SerializeField] private float _fov = 60;
	[SerializeField] private float _size = 25;
	[SerializeField] private float _near = 0.3f;
	[SerializeField] private float _far = 1000f;

	private Camera _cam;
	private Matrix4x4 _matrixOrtho;
	private Matrix4x4 _matrixPersp;
	private float _aspect;
	private bool _ortho;

	//public enum SmoothType
	//{
	//	Lerp,
	//	SmoothStep,
	//	SmootherStep,
	//	SmoothestStep
	//}

	private void Start()
	{
		_cam = this.GetComponent<Camera>();

		_aspect = _cam.aspect;
		_ortho = _cam.orthographic;
		_matrixOrtho = Matrix4x4.Ortho(-_size * _aspect, _size * _aspect, -_size, _size, _near, _far);
		_matrixPersp = Matrix4x4.Perspective(_fov, _cam.aspect, _near, _far);
	}

	private static Matrix4x4 LerpMatrix(Matrix4x4 pSrc, Matrix4x4 pDst, float pSmoothTime)
	{
		Matrix4x4 ret = new Matrix4x4();
		for (int i = 0; i < 16; i++)
		{
			ret[i] = Mathf.Lerp(pSrc[i], pDst[i], pSmoothTime);
		}

		return ret;
	}

	private static Matrix4x4 SmoothStepMatrix(Matrix4x4 pSrc, Matrix4x4 pDst, float pSmoothTime)
	{
		Matrix4x4 ret = new Matrix4x4();
		for (int i = 0; i < 16; i++)
		{
			ret[i] = Mathf.SmoothStep(pSrc[i], pDst[i], pSmoothTime);
		}

		return ret;
	}

	private static Matrix4x4 SmootherStepMatrix(Matrix4x4 pSrc, Matrix4x4 pDst, float pSmoothTime)
	{
		Matrix4x4 ret = new Matrix4x4();
		for (int i = 0; i < 16; i++)
		{
			ret[i] = MathfMore.SmootherStep(pSrc[i], pDst[i], pSmoothTime);
		}

		return ret;
	}

	private static Matrix4x4 SmoothestStepMatrix(Matrix4x4 pSrc, Matrix4x4 pDst, float pSmoothTime)
	{
		Matrix4x4 ret = new Matrix4x4();
		for (int i = 0; i < 16; i++)
		{
			ret[i] = MathfMore.SmoothestStep(pSrc[i], pDst[i], pSmoothTime);
		}

		return ret;
	}

	private IEnumerator SmoothFromTo(Matrix4x4 pSrc, Matrix4x4 pDst, float pSmoothTime, SmoothType pSmoothType)
	{
		for (float t = 0; t <= pSmoothTime; t += Time.deltaTime)
		{
			if (pSmoothType == SmoothType.Lerp)
			{
				_cam.projectionMatrix = LerpMatrix(pSrc, pDst, t / pSmoothTime);
			}
			else if (pSmoothType == SmoothType.SmoothStep)
			{
				_cam.projectionMatrix = SmoothStepMatrix(pSrc, pDst, t / pSmoothTime);
			}
			else if (pSmoothType == SmoothType.SmootherStep)
			{
				_cam.projectionMatrix = SmootherStepMatrix(pSrc, pDst, t / pSmoothTime);
			}
			else if (pSmoothType == SmoothType.SmoothestStep)
			{
				_cam.projectionMatrix = SmoothestStepMatrix(pSrc, pDst, t / pSmoothTime);
			}
			yield return new WaitForEndOfFrame(); ;
		}

		_cam.projectionMatrix = pDst;
	}

	private void BlendToMatrix(Matrix4x4 pDst, float pSmoothTime, SmoothType pSmoothType)
	{
		StopAllCoroutines();
		StartCoroutine(SmoothFromTo(_cam.projectionMatrix, pDst, pSmoothTime, pSmoothType));
	}

	public void SwitchMatrix(float pSmoothTime, SmoothType pSmoothType)
	{
		_ortho = !_ortho;
		if (_ortho) BlendToMatrix(_matrixOrtho, pSmoothTime, pSmoothType);
		else BlendToMatrix(_matrixPersp, pSmoothTime, pSmoothType);
	}
}
