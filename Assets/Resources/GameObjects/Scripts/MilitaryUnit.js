#pragma strict

public class MilitaryUnit extends MovableAttackableDamagable implements Unit{

	function clearCommands(){ //Clear Commands and Waypoints along with Waypoint objects
		for(var i:int=0;i<waypoints.Count;i++){ //Destroy all waypoint objects
			Destroy(waypoints[i].line.gameObject);
		}
		commands.Clear();
		waypoints.Clear();
	}

	function addCommand(hit:RaycastHit){ //Add Command and create new Waypoint
		var c:Command = ScriptableObject.CreateInstance.<Command>();
		c.hit = hit;
		if(commands.Count == 0)
			c.type = DecideandAct(hit);
		else
			c.type = Decide(hit);
		commands.Add(c);
		addWaypoint(hit);
	}
	
	function setCommand(hit:RaycastHit){ //Set first Command to new hit
		if(commands.Count > 0){ //Clear Commands
			clearCommands();
		}
		var c:Command = ScriptableObject.CreateInstance.<Command>();
		c.hit = hit;
		c.type = DecideandAct(hit);
		commands.Add(c);
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
			Waypoint.prev = commands[commands.Count-2].hit.point;
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
				return "Attack";
			}
			else{
				//Follow;
			}
		}
		//Ground
		else{
			return "Move";
		}
		return "";
	}
	
	function getParams(action:String, hit:RaycastHit):Object[]{
		var params:Object[];
		switch(action){
			case "Move":
			params = [hit, true];
			break;
			
			case "Attack":
			break;
		}
		return params;
	}
	
	function DecideandAct(hit:RaycastHit):String{
		var decision:String = Decide(hit);
		var action:Action = GetComponent(decision) as Action;
		if(action != null){
			var params:Object[] = getParams(decision, hit);
			action.Begin(params);
		}
		return decision;
	}
	
	function Act(){
		var params:Object[] = getParams(commands[0].type, commands[0].hit);
		var action:Action = GetComponent(commands[0].type) as Action;
		action.Begin(params);
	}
	
	function Selected(select:boolean){
		super.Selected(select);
		for(var i:int=0;i<waypoints.Count;i++){
			waypoints[i].line.enabled = select;
		}
	}
	
	function Update(){
		super.Update();
		if(commands.Count == 0) return;
		var a:Action = GetComponent(commands[0].type) as Action;
		if(a.Ended()){
			if(waypoints.Count == commands.Count){
				Destroy(waypoints[0].line.gameObject);
				waypoints.RemoveAt(0);
			}
			commands.RemoveAt(0);
			if(commands.Count != 0)
				Act();
			return;
		}
		if(waypoints.Count != 0)
			waypoints[0].line.SetPosition(0, transform.position);
	}
}