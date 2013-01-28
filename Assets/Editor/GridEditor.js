#pragma strict

@CustomEditor(Transform)
class GridEditor extends Editor{
	function OnInspectorGUI(){
		 DrawDefaultInspector();
	}
	function OnSceneGUI() {
		if(Selection.transforms.length == 0) return;
		for(var i:int=0;i<Selection.transforms.length;i++){
			if(Selection.transforms[i].GetComponent(Collider) == null) continue;
			var pos = Selection.transforms[i].position;
			var size:Vector3 = Selection.transforms[i].collider.bounds.size;
			var start:Vector3 = new Vector3(pos.x-(size.x/2)-EditorPrefs.GetInt("width"), pos.y, pos.z-(size.z/2)+(2*EditorPrefs.GetInt("height")));
			var end:Vector3 = new Vector3(pos.x-(size.x/2)-EditorPrefs.GetInt("width"), pos.y, pos.z-(size.z/2)+(2*EditorPrefs.GetInt("height")));
			Handles.color = Color.white;
			Handles.DrawLine(start, end);
		}
	}
}