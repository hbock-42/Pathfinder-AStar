using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraEffects : MonoBehaviour
{
	[SerializeField] private BlendMatrix _blender;

	private Camera _cam;

	private void Update()
	{
		InitiateSwitch();
	}

	public void InitiateSwitch()
	{
		if (Input.GetKeyDown(KeyCode.Q))
		{
			_blender.SwitchMatrix();
		}
	}

}
