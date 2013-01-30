#pragma strict

public class Waypoint extends ScriptableObject{
	static var prev:Vector3;
	static var pos:Vector3;
	public var renderer:Renderer;
	public var line:LineRenderer;
	function OnEnable (){
		var prefab:GameObject = Resources.Load("GameObjects/Prefabs/Waypoint");
		var waypoint:GameObject = Instantiate(prefab, Waypoint.pos, Quaternion(0,0,0,0));
		renderer = waypoint.renderer;
		line = waypoint.AddComponent(LineRenderer);
		line.enabled = true;
		line.SetPosition(0,Waypoint.prev);
	}
}