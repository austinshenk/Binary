#pragma strict
import System.Collections.Generic;

public class RTSPlayer extends MonoBehaviour{

	public static final var speed: int = 50;
	
	public static final var minAngularAmount:int = 1;
	public static final var angularSpeedFactor:int = 10;
	public static final var rotRange:int = 30;
	
	public static final var minZoomAmount:int = .5;
	public static final var zoomFactor:int = 4;
	public static final var minZoom:int = 50;
	public static final var maxZoom:int = 100;
	
	private var selectionBox:Transform;
	public var team:int = 0;
	public var alliance:int = 0;
	private var units:List.<RTSObject> = new List.<RTSObject>();
	private var multiSelection:boolean = false;
	private var selectedUnits2Save:RaycastHit[];
	private var tempUnitSelected:RTSObject;
	private var ray:Ray;
	private var hit:RaycastHit;
	private var sceneSelectionStartPoint:Vector3;
	private var screenSelectionStartPoint:Vector2;
	private var xUnitper3DUnit:float;
	private var yUnitper3DUnit:float;
	private var selectionEnded:boolean = true;
	public static final var minMouseDrag:int = 10;
	
	public var resourceNames:String[];
	public var resourceAmounts:int[];
	public var resources:Hashtable;
	
	public var playerGUISkin:GUISkin;
	public static final var mainGUIHeight:int = 150;
	public static final var topGUIHeight:int = 30;
	
	function calculateScreenConversion(){
		var ray1:Ray = camera.ScreenPointToRay(Vector3(0,0,0));
		var hit1:RaycastHit;
		var ray2:Ray = camera.ScreenPointToRay(Vector3(Screen.width,Screen.height,0));
		var hit2:RaycastHit;
		Physics.Raycast(ray1, hit1, Mathf.Infinity, 1<<8);
		Physics.Raycast(ray2, hit2, Mathf.Infinity, 1<<8);
		xUnitper3DUnit = Screen.width/Mathf.Abs(hit1.point.x-hit2.point.x);
		yUnitper3DUnit = Screen.height/Mathf.Abs(hit1.point.z-hit2.point.z);
	}
	
	function Start(){
		GameObject.FindGameObjectWithTag("GameManager").GetComponent(GameManager).addPlayer(this);
		selectionBox = transform.Find("Selection_Box");
		selectionBox.parent = null;
		calculateScreenConversion();
	}
	
	function move(dir:Vector3){
		transform.Translate(dir, Space.World);
	}
	
	function updateUnitsSelected(){
		for(var i:int=0;i<units.Count;i++){
			if(units[i] == null){
				units.RemoveAt(i);
			}
		}
	}
	
	function clearUnitsSelected(){
		for(var i:int=0;i<units.Count;i++){
			units[i].Selected(false);
		}
		units.Clear();
	}
	function clearUnitsSelected(index:int){
		for(var i:int=0;i<units.Count;i++){
			if(i==index) continue;
			units[i].Selected(false);
			units.RemoveAt(i);
		}
	}
	
	function mouseOverGUI():boolean{
		if(Input.mousePosition.y > mainGUIHeight){
			return false;
		}
		else{
			return true;
		}
	}
	
	function previewSelectedUnits(){
		multiSelection = (Vector2.Distance(screenSelectionStartPoint, Input.mousePosition) > minMouseDrag);
		if(multiSelection){    //Buffer added so it doesn't interfere wih single Selection
			//Cast Ray of current mouse position on Ground
			ray = camera.ScreenPointToRay(Input.mousePosition);
			if(Physics.Raycast(ray, hit, Mathf.Infinity, 1<<LayerConstants.GROUND)){
				//Set size of Selection Box
				selectionBox.localScale = Vector3(100,1,hit.point.z-sceneSelectionStartPoint.z);
				//Set position of Selection Box to account for the size
				selectionBox.position = Vector3(sceneSelectionStartPoint.x, sceneSelectionStartPoint.y, sceneSelectionStartPoint.z+(selectionBox.lossyScale.z/2));
				//Sweep the Selection Box in the direction and get colliders
				selectedUnits2Save = selectionBox.rigidbody.SweepTestAll(Vector3(hit.point.x-sceneSelectionStartPoint.x,0,0), Mathf.Abs(hit.point.x-sceneSelectionStartPoint.x));
				for(var i=0;i<selectedUnits2Save.length;i++){
					//Test if every collider is a Unit and on the same Team
					if(selectedUnits2Save[i].collider.GetComponent(Unit) == null){return;}
					if(selectedUnits2Save[i].collider.GetComponent(RTSObject).team == team){
						selectedUnits2Save[i].transform.GetComponent(RTSObject).peekSelected(true);
					}
				}
			}
		}
	}
	
	function saveSelectedUnits(){
		clearUnitsSelected();
		selectionEnded = true;
		for(var i=0;i<selectedUnits2Save.length;i++){
			if(selectedUnits2Save[i].collider.GetComponent(Unit) == null){continue;}
			units.Add(selectedUnits2Save[i].transform.GetComponent(RTSObject));
			selectedUnits2Save[i].transform.GetComponent(RTSObject).Selected(true);
		}
	}
	
	function Update(){
		updateUnitsSelected();
		
		//Moving and Selection Box Moving
		var pos = Time.deltaTime * speed;
		if(Input.GetButton("Vertical")){
			pos = Input.GetAxis("Vertical") * Mathf.Abs(pos);
			move(Vector3(0,0,pos));
			screenSelectionStartPoint.y -= pos*yUnitper3DUnit;
		}
		if(Input.GetButton("Horizontal")){
			pos = Input.GetAxis("Horizontal") * Mathf.Abs(pos);
			move(Vector3(pos,0,0));
			screenSelectionStartPoint.x -= pos*xUnitper3DUnit;
		}
		
		//Rotating
		if(Physics.Raycast(transform.position, transform.TransformDirection(Vector3(0,0,1)), hit, Mathf.Infinity, 1<<8)){
			//Cast a Ray to get a point to Rotate around
			var rotPoint = hit.point;
			var rotAmount:float = 0;
			var angle:int = transform.eulerAngles.y;    //Current Angle in Euler coordinates
			if(angle < 0){    //If Angle is somehow negative and small set it to 0
				angle = 0;
			}
			
			//Left
			//Convert angle to [-angle,angle] and store
			var tempangle1:float = angle;
			if(angle < 360 && angle >= 360-rotRange){
				tempangle1 = angle-360;
			}
			if(Input.GetKey("q")){
				if(tempangle1 < rotRange){    //If less than rotRange than rotate towards it
					rotAmount = (rotRange-tempangle1)/angularSpeedFactor;    //Decelerating amount
					if(rotRange-tempangle1 < minAngularAmount){     //If the change in rotation to the end is less than minimum change
						rotAmount = rotRange-tempangle1;            //Then set to end
					}
				}
			}
			else if(tempangle1 >= minAngularAmount && !Input.GetKey("e")){    //If it is not idle and E is not pressed
				rotAmount = -tempangle1/angularSpeedFactor;                   //Then Rotate back to start
			}
			
			//Right
			//Same thing as Left except angle is converted to [360-angle, 360+angle]
			var tempangle2:float = angle;
			if(angle >= 0 && angle <= rotRange){
				tempangle2 = angle+360;
			}
			if(Input.GetKey("e")){
				if(tempangle2 >= 360-rotRange){
					rotAmount = -(tempangle2-(360-rotRange))/angularSpeedFactor;
					if(360-rotRange-tempangle2 > -minAngularAmount){
						rotAmount = 360-rotRange-tempangle2;
					}
				}
			}
			else if(tempangle2 <= 360-minAngularAmount && !Input.GetKey("q")){
				rotAmount = (360-tempangle2)/angularSpeedFactor;
			}
			
			//Rotate the camera around the given (point, axis, amount)
			transform.RotateAround(rotPoint, Vector3(0,1,0), rotAmount);
		}
		
		//Zooming
		if(Input.GetAxis("Mouse ScrollWheel") != 0){
			//Zoom In
			if(Input.GetAxis("Mouse ScrollWheel") > 0){
				if(transform.position.y <= minZoom+minZoomAmount){    //If height less than buffer amount + minimum height
					pos = minZoom-transform.position.y;               //Then set height to minimum height
				}
				else{                                                 //Else set height to normal zoom speed
					pos = (minZoom-transform.position.y)/zoomFactor;
				}
			}
			//Zoom Out
			if(Input.GetAxis("Mouse ScrollWheel") < 0){               //Same as zoom in except it checks the maximum height
				if(transform.position.y >= maxZoom-minZoomAmount){
					pos = maxZoom-transform.position.y;
				}
				else{
					pos = (maxZoom-transform.position.y)/zoomFactor*2;//Multiplied by 2 so that the player reaches the top quickly
				}
			}
			//Translate Y amount
			move(Vector3(0,pos,0));
			//Recalculate screen conversions
			calculateScreenConversion();
		}
		
		//Single Unit Selection
		if(Input.GetButtonDown("Select") && !mouseOverGUI()){
			//Cast Ray to hit Ground
			ray = camera.ScreenPointToRay(Input.mousePosition);
			if(Physics.Raycast(ray, hit, Mathf.Infinity, 1<<LayerConstants.GROUND)){
				sceneSelectionStartPoint = hit.point;    //Store initial hit
			}
			//Cast Ray to Select Unit
			if(Physics.Raycast(ray, hit, Mathf.Infinity, 1<<LayerConstants.OBJECTS)){
				//Store Unit in Selectables
				clearUnitsSelected();
				selectionEnded = false;
				hit.collider.GetComponent(RTSObject).Selected(true);
				selectedUnits2Save = new RaycastHit[1];
				selectedUnits2Save[0] = hit;
			}
			//Store the Mouse position
			screenSelectionStartPoint = Input.mousePosition;
		}
		
		//Multiple Unit Selection
		if(Input.GetButton("Select") && !mouseOverGUI()){
			previewSelectedUnits();
		}
		
		//End Selection
		if(Input.GetButtonUp("Select")){
			selectionEnded = true;
			if(multiSelection){
				saveSelectedUnits();
			}
			else if(selectedUnits2Save.Length == 1){
				units.Add(selectedUnits2Save[0].collider.GetComponent(RTSObject));
			}
			selectedUnits2Save = new RaycastHit[0];
			//Hide Selection Box off screen
			selectionBox.localScale = Vector3(1,1,1);
			selectionBox.position = Vector3(0,0,0);
		}
		
		//Command Selected Units
		if(Input.GetButtonDown("Command") && !mouseOverGUI()){
			//If Units to Command and atleast the first one is on player's Team
			tempUnitSelected = units[0];
			if(units.Count != 0 && tempUnitSelected.team == team){
				//Cast Ray on every object in scene
				ray = camera.ScreenPointToRay(Input.mousePosition);
				if(Physics.Raycast(ray, hit, Mathf.Infinity, 1<<LayerConstants.OBJECTS) || Physics.Raycast(ray, hit, Mathf.Infinity, 1<<LayerConstants.GROUND)){
					UnitManager.sendUnitInfo(units, hit, Input.GetButton("Consecutive Command"));
				}
			}
		}
	}//END UPDATE BLOCK
	
	function OnGUI(){
		GUI.skin = playerGUISkin;
		
		//Draw a GUI Selection Box with Custom Style
		if(Input.GetButton("Select") && Vector2.Distance(screenSelectionStartPoint, Input.mousePosition) > 10 && !mouseOverGUI()){
			//Screen coordinates are bottom-left is (0,0) and top-right is (Screen.width, Screen.height)
			GUI.Box(Rect(screenSelectionStartPoint.x, Screen.height-screenSelectionStartPoint.y, Input.mousePosition.x-screenSelectionStartPoint.x, -(Input.mousePosition.y-screenSelectionStartPoint.y)), "", playerGUISkin.customStyles[0]);
		}
		
		//Top GUI Bar
		GUILayout.BeginArea(new Rect(0,0,Screen.width,topGUIHeight));
			GUILayout.BeginHorizontal();
				//Menu Bar
				GUILayout.BeginArea(new Rect(0,0,200,topGUIHeight));
				GUILayout.EndArea();
				GUILayout.FlexibleSpace();
				//Resources
				GUILayout.BeginArea(new Rect(Screen.width-200,0,200,topGUIHeight));
					GUILayout.BeginHorizontal();
					for(var i=0;i<resourceNames.length;i++){
						GUILayout.Label(resourceNames[i]+": "+resourceAmounts[i]);
					}
					GUILayout.EndHorizontal();
				GUILayout.EndArea();
			GUILayout.EndHorizontal();
		GUILayout.EndArea();
		
		//Main player GUI
		GUILayout.BeginArea(new Rect(0,Screen.height-mainGUIHeight,Screen.width,mainGUIHeight));
		//GUI.Box(new Rect(0,0,Screen.width, mainGUIHeight), "");
			//Minimap
			//Selected Units
			GUILayout.BeginHorizontal();
			GUILayout.Space(mainGUIHeight);
			var guiSelectWidth:int = Screen.width-(2*mainGUIHeight);
			GUILayout.BeginArea(new Rect(mainGUIHeight,0,guiSelectWidth,mainGUIHeight));
			GUILayout.Box("", GUILayout.Width(Screen.width-(2*mainGUIHeight)), GUILayout.Height(mainGUIHeight));
				if(units.Count > 0 && selectionEnded){
					var xoffset:int = 0;
					var yoffset:int = 0;
					var selectedHeight:int = 40;
					var selectedWidth:int = 40;
					var tempUnit:RTSObject[] = new RTSObject[1];
					var unitSelectWidth:int = 480;
					GUILayout.BeginArea(new Rect((guiSelectWidth-unitSelectWidth)/2,0,unitSelectWidth,mainGUIHeight));
					if(units.Count == 1){
						tempUnitSelected = units[0];
						GUILayout.BeginArea(new Rect(0,0,unitSelectWidth/2, mainGUIHeight));
						GUILayout.BeginVertical();
							if(tempUnitSelected.transform.GetComponent(Unit) != null){
								GUILayout.Label(tempUnitSelected.icon, GUILayout.Height(mainGUIHeight-50));
								GUILayout.Label(tempUnitSelected.GetComponent(Damagable).getHealth() + "/" + tempUnitSelected.GetComponent(Damagable).getMaxHealth(), "unitHealthNumber");
							}
							else if(tempUnitSelected.transform.GetComponent(Resource) != null){
								GUILayout.Label(tempUnitSelected.icon, GUILayout.Height(mainGUIHeight-50));
								var r:Resource = tempUnitSelected.transform.GetComponent(Resource);
								for(i=0;i<r.resourcesAllowed.Count;i++){
									GUILayout.Label(r.resourcesAllowed[i] +" "+ r.currentResources[i], "unitHealthNumber");
								}
							}
						GUILayout.EndVertical();
						GUILayout.EndArea();
						GUILayout.BeginArea(new Rect(unitSelectWidth/2,0,unitSelectWidth/2, mainGUIHeight));
						GUILayout.BeginVertical();
							GUILayout.Label(tempUnitSelected.named, "unitName");
						GUILayout.EndVertical();
						GUILayout.EndArea();
					}
					else{
						for(var a:int=0;a<units.Count;a++){
							tempUnitSelected = units[a];
							if(tempUnitSelected != null){
								if(GUI.Button(new Rect(xoffset,yoffset,selectedWidth,selectedHeight), tempUnitSelected.icon)){
									if(Event.current.button == 0){
										clearUnitsSelected(a);
										tempUnitSelected.Selected(true);
									}
									else if(Event.current.button == 1){
										tempUnitSelected.Selected(false);
										units.RemoveAt(a);
									}
								}
								if(xoffset+(2*selectedWidth) > unitSelectWidth){
									xoffset = 0;
									yoffset += selectedHeight;
								}
								else{
									xoffset += selectedWidth;
								}
							}
						}
					}
					GUILayout.EndArea();
				}
			GUILayout.EndArea();
			//Actions
			GUILayout.BeginArea(new Rect(Screen.width-mainGUIHeight,0,mainGUIHeight,mainGUIHeight));
			GUILayout.Box("", GUILayout.MinWidth(mainGUIHeight), GUILayout.Height(mainGUIHeight));
				if(units.Count != 0 && selectionEnded){
					xoffset = 0;
					yoffset = 0;
					var actionHeight:int = 50;
					var actionWidth:int = 50;
					tempUnitSelected = units[0];
					if(tempUnitSelected.team == team){
						for(a=0;a<tempUnitSelected.actions.length;a++){
							if(tempUnitSelected.actions[a] != null){
								var actionName:String = tempUnitSelected.actions[a].name;
								if(GUI.Button(new Rect(xoffset,yoffset,actionWidth,actionHeight), actionName)){
									var params:Object[] = [actionName, units];
									tempUnitSelected.GetComponent(actionName).SendMessage("findBestCasters", params as Object[]);
								}
							}
							else{
								GUI.Label(new Rect(xoffset,yoffset,actionWidth,actionHeight), "");
							}
							if(a%3 == 2){
								xoffset = 0;
								yoffset += actionHeight;
							}
							else{
								xoffset += actionWidth;
							}
						}
					}
				}
			GUILayout.EndArea();
			GUILayout.EndHorizontal();
		//End of player GUI
		GUILayout.EndArea();
	}//END ONGUI BLOCK
	
	function requestHoverPoint():Vector3{
		ray = camera.ScreenPointToRay(Input.mousePosition);
		if(Physics.Raycast(ray, hit, Mathf.Infinity, 1<<LayerConstants.GROUND) && !mouseOverGUI()) return hit.point;
		else return hit.point;
	}
	
	function requestCommandPoint():RaycastHit{
		return calculateHitPoint();
	}
	
	function requestTarget(layer:int):RaycastHit{
		ray = camera.ScreenPointToRay(Input.mousePosition);
		if(Physics.Raycast(ray, hit, Mathf.Infinity, 1<<layer) && !mouseOverGUI()) return hit;
		else return hit;
	}
	
	function calculateHitPoint():RaycastHit{
		ray = camera.ScreenPointToRay(Input.mousePosition);
		var layer:int = 1<<LayerConstants.MINIMAP;
		layer = ~layer;
		if(Physics.Raycast(ray, hit, Mathf.Infinity, layer) && !mouseOverGUI()) return hit;
		else return hit;
	}
}