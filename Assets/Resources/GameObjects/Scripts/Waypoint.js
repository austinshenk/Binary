#pragma strict

public class Waypoint extends ScriptableObject{
	static var prefab:GameObject = Resources.Load("GameObjects/Prefabs/Waypoint") as GameObject;
	static var prev:Vector3;
	static var pos:Vector3;
	public var line:LineRenderer;
	function OnEnable (){
		var waypoint:GameObject = Instantiate(prefab, Waypoint.pos, Quaternion(0,0,0,0));
		line = waypoint.AddComponent(LineRenderer);
		line.enabled = true;
		line.SetPosition(0,Waypoint.prev);
		line.SetPosition(1,Waypoint.pos);
	}
}