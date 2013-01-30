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
	public var sightRange:float = 0;
	public var priority:float = 0;
	public var waypoints:List.<Waypoint> = new List.<Waypoint>();
	public var commands:List.<Command> = new List.<Command>();
	private var selectionPlaneRenderer:Renderer;
	
	function Start(){ //Execute initital actions like team color and Actions
		//Set color for Minimap display
		renderer.material.color = Selectable.colorID[team];
		Minimap.addToMinimap(this.gameObject);
		selectionPlaneRenderer = transform.Find("SelectedPlane").GetComponent(Renderer);
		
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
	}
	
	function isSelected():boolean{ //Return state of renderer for selection plane
		return selectionPlaneRenderer.enabled;
	}
	
	function addCommand(hit:RaycastHit){}
	
	function setCommand(hit:RaycastHit){}
	
	function Kill(){ //Destroy this
		Destroy(this.gameObject);
	}
}
public class Command extends System.ValueType{
	var hit:RaycastHit;
	var type:String;
	
	public function Command(hit:RaycastHit, type:String){
		this.hit = hit;
		this.type = type;
	}
}