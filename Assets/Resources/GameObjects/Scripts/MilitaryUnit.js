#pragma strict

public class MilitaryUnit extends MovableAttackableDamagable implements Unit{

	function clearWaypoints(){ //Clear Commands and Waypoints along with Waypoint objects
		for(var i=0;i<waypoints.Count;i++){ //Destroy all waypoint objects
			Destroy(waypoints[i]);
		}
		commands.Clear();
		waypoints.Clear();
	}

	function addCommand(hit:RaycastHit){ //Add Command and create new Waypoint
		commands.Add(new Command(hit, Decide(hit)));
		addWaypoint(hit);
	}
	
	function setCommand(hit:RaycastHit){ //Set first Command to new hit
		if(commands.Count > 0){ //Clear Commands
			clearWaypoints();
		}
		commands.Add(new Command(hit, DecideandAct(hit)));
	}
	
	function addWaypoint(hit:RaycastHit){
		var pos:Vector3 = hit.point;
		pos.y += .5;
		var newWaypoint:Waypoint;
		if(commands.Count <= 1){
			Waypoint.prev = pos;
			Waypoint.pos = pos;
			newWaypoint = ScriptableObject.CreateInstance.<Waypoint>();
		}
		else{
			Waypoint.prev = commands[commands.Count-1].hit.point;
			Waypoint.pos = pos;
			newWaypoint = ScriptableObject.CreateInstance.<Waypoint>();
		}
		waypoints.Add(newWaypoint);
	}
	
	function Decide(hit:RaycastHit):String{
		var target:Collider = hit.collider;
		//Some type of Unit or Building
		if(target.GetComponent(RTSObject) != null){
			if(target.GetComponent(RTSObject).alliance != 0 && target.GetComponent(RTSObject).alliance != alliance){
				return "Attackable";
			}
			else{
				return "Follow";
			}
		}
		//Ground
		else{
			return "Movable";
		}
	}
	
	function DecideandAct(hit:RaycastHit):String{
		var decision:String = Decide(hit);
		switch(decision){
			case "Movable":
			Move(hit.point, true);
			break;
			
			case "Attackable":
			break;
		}
		return decision;
	}
	
	function Selected(select:boolean){
		super.Selected(select);
		for(var i:int=0;i<waypoints.Count;i++){
			waypoints[i].renderer.enabled = select;
		}
	}
	
	function Update(){
		if(waypoints.Count == 0) return;
		if(GetComponent(commands[0].type).Ended()){
			Debug.Log("Ended");
		}
		waypoints[0].line.SetPosition(0, transform.position);
	}
}