#pragma strict

public class Waypoint extends MonoBehaviour{
	function Waypoint(prev:Vector3, pos:Vector3){
		var prefab:GameObject = Resources.Load("Game_Objects/Prefabs/Waypoint");
		var waypoint:GameObject = Instantiate(prefab, pos, Quaternion(0,0,0,0));
		var line:LineRenderer = waypoint.AddComponent(LineRenderer);
		line.SetPosition(0,prev);
	}
}