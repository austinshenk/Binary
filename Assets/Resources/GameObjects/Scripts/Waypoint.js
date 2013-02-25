#pragma strict

public class Waypoint extends ScriptableObject{
	static var prefab:GameObject = Resources.Load("GameObjects/Prefabs/Waypoint") as GameObject;
	static var prev:Vector3;
	static var pos:Vector3;
	public var line:LineRenderer;
	public var vertexCount:int = 0;
	public var positions:Vector3[];
	function create(prev:Vector3, pos:Vector3){
		if(vertexCount == 0){
			var waypoint:GameObject = Instantiate(prefab, Waypoint.pos, Quaternion(0,0,0,0));
			line = waypoint.GetComponent(LineRenderer);
			line.SetPosition(0,prev);
			line.SetPosition(1,pos);
			positions = new Vector3[2];
			positions[0] = prev;
			positions[1] = pos;
			vertexCount = 2;
		}
		else{
			var temp:Vector3[] = new Vector3[vertexCount+1];
			for(var i:int=0;i<vertexCount;i++){
				temp[i] = positions[i];
			}
			temp[vertexCount] = pos;
			positions = temp;
			line.SetVertexCount(vertexCount+1);
			line.SetPosition(vertexCount, pos);
			vertexCount++;
		}
	}
	function pop(){
		if(vertexCount == 2){
			destroy();
			return;
		}
		vertexCount--;
		line.SetVertexCount(vertexCount);
		var temp:Vector3[] = new Vector3[vertexCount];
		for(var i:int=0;i<vertexCount;i++){
			line.SetPosition(i, positions[i+1]);
			temp[i] = positions[i+1];
		}
		positions = temp;
	}
	function destroy(){
		if(line == null) return;
		Destroy(line.gameObject);
		line == null;
		vertexCount = 0;
	}
}