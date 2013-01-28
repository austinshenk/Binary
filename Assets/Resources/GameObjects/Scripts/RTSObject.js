#pragma strict

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
	public var waypoints = new Array();
	public var commands = new Array();
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
	
	function Selected(select:boolean){ //Set Object's selection plane to new select state
		selectionPlaneRenderer.enabled = select;
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