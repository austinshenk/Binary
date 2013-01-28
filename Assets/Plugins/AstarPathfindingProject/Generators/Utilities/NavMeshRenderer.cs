using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class NavMeshRenderer : MonoBehaviour {
	
	string lastLevel = "";
	
	/** Used to get rid of the compiler warning that #lastLevel is not used */
	public string SomeFunction () {
		return lastLevel;
	}
	
	// Update is called once per frame
	void Update () {
		#if UNITY_EDITOR
		if (lastLevel == "") {
			lastLevel = EditorApplication.currentScene;
		}
		
		if (lastLevel != EditorApplication.currentScene) {
			Debug.Log ("Level change "+lastLevel+" --> "+EditorApplication.currentScene);
			DestroyImmediate (gameObject);
		}
		#endif
	}
}