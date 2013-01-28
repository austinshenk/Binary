#pragma strict

public class GridSnapper extends EditorWindow{
	var width:int = EditorPrefs.GetInt("width");
	var height:int = EditorPrefs.GetInt("height");
	@MenuItem ("Window/Grid Snapper")
	static function Init () { 
        var window = ScriptableObject.CreateInstance.<GridSnapper>();
		window.Show();
    }
	
	function snapPosition(position:Vector3):Vector3{
		var XOffset:float = position.x%width;
		var ZOffset:float = position.z%height;
		if(XOffset > width/2.0){
			position.x = position.x+(width-XOffset);
		}
		else{
			position.x = position.x-XOffset;
		}
		if(ZOffset > height/2.0){
			position.z = position.z+(height-ZOffset);
		}
		else{
			position.z = position.z-ZOffset;
		}
		return position;
	}
	
	function OnGUI(){
		width = EditorGUILayout.IntField("Width:", width);
		height = EditorGUILayout.IntField("Height:", height);
		if(width < 1) width = 1;
		if(height < 1) height = 1;
		EditorPrefs.SetInt("width", width);
		EditorPrefs.SetInt("height", height);
	}
	
	function Update(){
		if(Selection.transforms.length == 0) return;
		for(var i:int=0;i<Selection.transforms.length;i++){
			Selection.transforms[i].position = snapPosition(Selection.transforms[i].position);
		}
	}
}