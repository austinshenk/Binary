#pragma strict

public class MilitaryUnit extends MovableAttackableDamagable implements Unit{

	function clearWaypoints(){ //Clear Commands and Waypoints along with Waypoint objects
		for(var i=0;i<waypoints.length;i++){ //Destroy all waypoint objects
			Destroy(waypoints[i]);
		}
		commands.Clear();
		waypoints.Clear();
	}

	function addCommand(hit:RaycastHit){ //Add Command and create new Waypoint
		commands.Add(hit);
		addWaypoint();
	}
	
	function setCommand(hit:RaycastHit){ //Set first Command to new hit
		if(commands.length > 0){ //Clear Commands
			clearWaypoints();
		}
		commands.Add(hit);
		DecideandAct(hit);
	}
	
	function addWaypoint(){
		var index:int = commands.length-1;
		var hit:RaycastHit = commands[index];
		var pos:Vector3 = hit.point;
		pos.y += .25;
		var newWaypoint:Waypoint;
		if(commands.length <= 1){
			newWaypoint = new Waypoint(pos, pos);
		}
		else{
			newWaypoint = new Waypoint(commands[index-1], pos);
		}
	}
	
	function Decide(hit:RaycastHit):String{
		var target:Collider = hit.collider;
		//Some type of Unit or Building
		if(target.GetComponent(RTSObject) != null){
			if(target.GetComponent(RTSObject).alliance != 0 && target.GetComponent(RTSObject).alliance != alliance){
				return "Attack";
			}
			else{
				return "Follow";
			}
		}
		//Ground
		else{
			return "Move";
		}
	}
	
	function DecideandAct(hit:RaycastHit){
		var decision:String = Decide(hit);
		switch(decision){
			case "Move":
			Move(hit.point, true);
			break;
			
			case "Attack":
			break;
		}
	}
}