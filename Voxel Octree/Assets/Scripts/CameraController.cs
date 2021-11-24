using System;
using UnityEngine;

namespace UnityTemplateProjects
{
	public class CameraController : MonoBehaviour
	{
		public float speed = 0;
		public float pitch = 0f;
		public float yaw = 0f;
		
		private void Update()
		{
			pitch -= Input.GetAxis("Mouse Y");
			yaw += Input.GetAxis("Mouse X");
			transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
			
			Vector3 dir = Vector3.zero;
			if (Input.GetKey(KeyCode.W))
				dir += transform.forward;
			if (Input.GetKey(KeyCode.A))
				dir -= transform.right;
			if (Input.GetKey(KeyCode.S))
				dir -= transform.forward;
			if (Input.GetKey(KeyCode.D))
				dir += transform.right;
			if (Input.GetKey(KeyCode.Space))
				dir += transform.up;
			if (Input.GetKey(KeyCode.LeftControl))
				dir -= transform.up;
			dir = Vector3.Normalize(dir);
			
			var tempSpeed = 1f;
			if (Input.GetKey(KeyCode.LeftShift))
				tempSpeed = 2f;
			transform.position += dir * (Time.deltaTime * Mathf.Pow(2, speed) * tempSpeed);

			speed += Input.mouseScrollDelta.y * 0.2f;
		}
	}
}