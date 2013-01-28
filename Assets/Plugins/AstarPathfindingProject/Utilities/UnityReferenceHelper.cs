using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class UnityReferenceHelper : MonoBehaviour {
	
	//[HideInInspector]
	[SerializeField]
	private string guid;
	
	public string GetGUID () {
		return guid;
	}
	
	public void Awake () {
		Reset ();
	}
	
	public void Reset () {
		if (guid == null || guid == "") {
			guid = System.Guid.NewGuid ().ToString ();
			Debug.Log ("Created new GUID - "+guid);
		} else {
			foreach (UnityReferenceHelper urh in FindObjectsOfType (typeof(UnityReferenceHelper)) as UnityReferenceHelper[]) {
				if (urh != this && guid == urh.guid) {
					guid = System.Guid.NewGuid ().ToString ();
					Debug.Log ("Created new GUID - "+guid);
					return;
				}
			}
		}
	}
}
