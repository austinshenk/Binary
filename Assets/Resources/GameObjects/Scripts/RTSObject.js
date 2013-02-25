#pragma strict
import System.Collections.Generic;

public class RTSObject extends MonoBehaviour implements Commandable{
	public var named:String;
	public var icon:Texture2D;
	public var team:int = 0;
	public var alliance:int = 0;
	public var buildTime:int = 0;
	public var height:float = 0; 
	public var actions:TextAsset[] = new TextAsset[9];
	public var hiddenActions:TextAsset[] = new TextAsset[0];
	public var resourceNames:String[] = new String[0];
	public var resourceAmounts:int[] = new int[0];
	public var sightRange:float = 0;
	public var priority:float = 0;
	public var waypoint:Waypoint;
	public var commands:List.<Command> = new List.<Command>();
	private var selectionPlaneRenderer:Renderer;
	
	function Start(){ //Execute initital actions like team color and Actions
		//Set color for Minimap display
		renderer.material.color = Selectable.colorID[team];
		Minimap.addToMinimap(this.gameObject);
		selectionPlaneRenderer = transform.Find("SelectedPlane").GetComponent(Renderer);
		waypoint = ScriptableObject.CreateInstance.<Waypoint>();
		
		for(var i=0;i<actions.length;i++){ //Add if Action exists
			if(actions[i] != null){ //Add Action to this
				gameObject.AddComponent(actions[i].name);
			}
		}
		for(i=0;i<hiddenActions.length;i++){ //Add if Action exists as hidden
			if(hiddenActions[i] != null){ //Add Action as Hidden to this
				gameObject.AddComponent(hiddenActions[i].name);
			}
		}
		hiddenActions = null; //Clear the memory allocation
	}
	
	function peekSelected(select:boolean){ //Set Object's selection plane to new select state
		selectionPlaneRenderer.enabled = select;
	}
	
	function Selected(select:boolean){
		peekSelected(select);
		if(waypoint.line != null)
			waypoint.line.enabled = select;
	}
	
	function isSelected():boolean{ //Return state of renderer for selection plane
		return selectionPlaneRenderer.enabled;
	}
	
	function Kill(){ //Destroy this
		Destroy(this.gameObject);
	}
	function clearCommands(){ //Clear Commands and Waypoints along with Waypoint objects
		commands.Clear();
		waypoint.destroy();
	}

	function addCommand(hit:RaycastHit, multiCommand:boolean){ //Add Command and create new Waypoint
		if(!multiCommand){
			setCommand(hit);
			return;
		}
		var c:Command = ScriptableObject.CreateInstance.<Command>();
		c.hit = hit;
		if(commands.Count == 0)
			c.type = DecideandAct(hit);
		else
			c.type = Decide(hit);
		commands.Add(c);
		hit.point.y += .5;
		if(commands.Count <= 1){
			waypoint.create(hit.point, hit.point);
		}
		else{
			waypoint.create(commands[commands.Count-2].hit.point, hit.point);
		}
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
	
	function Update(){
		if(commands.Count == 0) return;
		var a:Action = GetComponent(commands[0].type) as Action;
		if(a == null) return;
		if(a.Ended()){
			if(waypoint.vertexCount-1 == commands.Count){
				waypoint.pop();
			}
			commands.RemoveAt(0);
			if(commands.Count != 0)
				Act();
			return;
		}
		if(waypoint.line != null)
			waypoint.line.SetPosition(0, transform.position);
	}
}