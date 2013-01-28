using UnityEngine;
using System.Collections;

public class ShadowAlign : MonoBehaviour {
	
	public Light lightSource;
	
	// Update is called once per frame
	void LateUpdate () {
		if (lightSource.type == LightType.Directional) {
			transform.rotation = lightSource.transform.rotation;
		} else {
			transform.LookAt (transform.position+(transform.position-lightSource.transform.position));
		}
	}
}
