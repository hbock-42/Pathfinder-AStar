using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Smooth;

[RequireComponent(typeof(Camera))]
public class CameraEffects : MonoBehaviour
{
	[SerializeField] private Vector3 _orthoPosition;
	[SerializeField] private Vector3 _orthoRotation;
	[SerializeField] private Vector3 _perspPosition;
	[SerializeField] private Vector3 _perpRotation;
	[SerializeField] private float _smoothTimeOrthoToPers = 0.5f;
	[SerializeField] private float _smoothTimePersToOrtho = 0.5f;

	[SerializeField] private BlendMatrix _blender;

	private Camera _cam;
	private bool _orhto;


	#region Singleton
	private static CameraEffects _instance;
	public static CameraEffects Instance { get { return _instance; } }

	private void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
		}
		else if (_instance != this)
		{
			Destroy(this.gameObject);
		}
	}
	#endregion

	private void Start()
	{
		_cam = this.GetComponent<Camera>();
		_orhto = _cam.orthographic;
	}

	private void Update()
	{
		InitiateSwitch();
	}

	public void InitiateSwitch()
	{
		if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R))
		{
			_orhto = !_orhto;
			float smoothTime = _orhto ? _smoothTimePersToOrtho : _smoothTimeOrthoToPers;
			if (Input.GetKeyDown(KeyCode.Q))
			{
				_blender.SwitchMatrix(smoothTime, SmoothType.Lerp);
				BlendToTransform(smoothTime, SmoothType.Lerp);
			}
			else if (Input.GetKeyDown(KeyCode.W))
			{
				_blender.SwitchMatrix(smoothTime, SmoothType.SmoothStep);
				BlendToTransform(smoothTime, SmoothType.SmoothStep);
			}
			else if (Input.GetKeyDown(KeyCode.E))
			{
				_blender.SwitchMatrix(smoothTime, SmoothType.SmootherStep);
				BlendToTransform(smoothTime, SmoothType.SmootherStep);
			}
			else if (Input.GetKeyDown(KeyCode.R))
			{
				_blender.SwitchMatrix(smoothTime, SmoothType.SmoothestStep);
				BlendToTransform(smoothTime, SmoothType.SmoothestStep);
			}
		}
	}

	private void BlendToTransform(float pSmoothTime, SmoothType pSmoothType)
	{
		StopAllCoroutines();
		if (_orhto)
		{
			StartCoroutine(SmoothFromTo(_cam.transform.position, _orthoPosition, _cam.transform.rotation.eulerAngles, _orthoRotation, pSmoothTime, pSmoothType));
		}
		else
		{
			StartCoroutine(SmoothFromTo(_cam.transform.position, _perspPosition, _cam.transform.rotation.eulerAngles, _perpRotation, pSmoothTime, pSmoothType));
		}
	}

	IEnumerator SmoothFromTo(Vector3 pPositionStart, Vector3 pPositionEnd, Vector2 pRotationStart, Vector2 pRotationEnd, float pSmoothTime, SmoothType pSmoothType)
	{
		for (float t = 0; t <= pSmoothTime; t += Time.deltaTime)
		{
			if (pSmoothType == SmoothType.Lerp)
			{
				_cam.transform.position = Vector3.Lerp(pPositionStart, pPositionEnd, t / pSmoothTime);
				_cam.transform.rotation = Quaternion.Euler(Vector3.Lerp(pRotationStart, pRotationEnd, t / pSmoothTime));
			}
			else if (pSmoothType == SmoothType.SmoothStep)
			{
				_cam.transform.position = SmoothStepV3(pPositionStart, pPositionEnd, t / pSmoothTime);
				_cam.transform.rotation = Quaternion.Euler(SmoothStepV3(pRotationStart, pRotationEnd, t / pSmoothTime));
			}
			else if (pSmoothType == SmoothType.SmootherStep)
			{
				_cam.transform.position = SmoothertStepV3(pPositionStart, pPositionEnd, t / pSmoothTime);
				_cam.transform.rotation = Quaternion.Euler(SmoothertStepV3(pRotationStart, pRotationEnd, t / pSmoothTime));
			}
			else if (pSmoothType == SmoothType.SmoothestStep)
			{
				_cam.transform.position = SmoothestStepV3(pPositionStart, pPositionEnd, t / pSmoothTime);
				_cam.transform.rotation = Quaternion.Euler(SmoothestStepV3(pRotationStart, pRotationEnd, t / pSmoothTime));
			}
			yield return new WaitForEndOfFrame(); ;
		}
	}

	#region V3 Smooth

	private static Vector3 SmoothStepV3(Vector3 pStart, Vector3 pEnd, float pSmoothTime)
	{
		Vector3 ret = new Vector3();
		for (int i = 0; i < 3; i++)
		{
			ret.x = Mathf.SmoothStep(pStart.x, pEnd.x, pSmoothTime);
			ret.y = Mathf.SmoothStep(pStart.y, pEnd.y, pSmoothTime);
			ret.z = Mathf.SmoothStep(pStart.z, pEnd.z, pSmoothTime);
		}

		return ret;
	}

	private static Vector3 SmoothertStepV3(Vector3 pStart, Vector3 pEnd, float pSmoothTime)
	{
		Vector3 ret = new Vector3();
		for (int i = 0; i < 3; i++)
		{
			ret.x = MathfMore.SmootherStep(pStart.x, pEnd.x, pSmoothTime);
			ret.y = MathfMore.SmootherStep(pStart.y, pEnd.y, pSmoothTime);
			ret.z = MathfMore.SmootherStep(pStart.z, pEnd.z, pSmoothTime);
		}

		return ret;
	}

	private static Vector3 SmoothestStepV3(Vector3 pStart, Vector3 pEnd, float pSmoothTime)
	{
		Vector3 ret = new Vector3();
		for (int i = 0; i < 3; i++)
		{
			ret.x = MathfMore.SmoothestStep(pStart.x, pEnd.x, pSmoothTime);
			ret.y = MathfMore.SmoothestStep(pStart.y, pEnd.y, pSmoothTime);
			ret.z = MathfMore.SmoothestStep(pStart.z, pEnd.z, pSmoothTime);
		}

		return ret;
	}
	#endregion

}
