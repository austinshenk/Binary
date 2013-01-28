using UnityEngine;
using System.Collections;
using Pathfinding;

public class DoorController : MonoBehaviour {
	
	private bool open = false;
	
	public int bitToChange = 0;
	
	Bounds bounds;
	
	public void Start () {
		bounds = collider.bounds;
		SetState (open);
	}
	
	// Use this for initialization
	void OnGUI () {
		
		if (GUILayout.Button ("Toggle Door")) {
			SetState (!open);
		}
	}
	
	public void SetState (bool open) {
		this.open = open;
		
		GraphUpdateObject guo = new GraphUpdateObject(bounds);
		guo.tagsChange = 1 << bitToChange;
		
		guo.tagsValue = open ? 1 << bitToChange : 0;
		
		AstarPath.active.UpdateGraphs (guo);
		
		if (open) {
			animation.Play ("Open");
		} else {
			animation.Play ("Close");
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
