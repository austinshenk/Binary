#pragma strict
import System.Collections.Generic;

public class Building extends StationaryNeutralDamagable implements Buildable{
	private var defaultSpawnPoint:Vector3;
	private var possibleSpawnPointsObstacles:List.<Vector3> = new List.<Vector3>();							 //The spawn points found when checking against Obstacles
	private var possibleSpawnPointsUnits:List.<Vector3> = new List.<Vector3>();    							 //Spawn points found when checking against the Selectables layer
	private var buildQue:List.<GameObject> = new List.<GameObject>();            //Collection of Units the Building should create
	public var queMax:int = 5;                      							 //Maximum number of Units that can be qued at one time
	private var currentQue:int = 0;                     						 //The current number of qued Units
	private var currentBuildTime:float = 0;            							 //The amount of time the current Unit has been building
	public var resourceCapable:boolean = false;
	public var resourcesNamesAllowed:String[];
	public var resourcesAllowed:Hashtable = new Hashtable();
	
	/**********VIRTUAL FUNCTIONS************/
	function addCommand(hit:RaycastHit, multiCommand:boolean){
		if(!multiCommand){
			setCommand(hit);
			return;
		}
		var c:Command = ScriptableObject.CreateInstance.<Command>();
		c.hit = hit;
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
	function setCommand(hit:RaycastHit){
		super.setCommand(hit);
		hit.point.y += .5;
		waypoint.create(transform.position, hit.point);
	}
	/**************************************/
	
	function Start(){
		super.Start();
		//Find the possible spawn points in the Obstacle layer
		findPossibleSpawnPoints(4);
		defaultSpawnPoint = Vector3(transform.position.x-(collider.bounds.size.x/2)-2, transform.position.y-(collider.bounds.size.y/2), transform.position.z-(collider.bounds.size.z/2)-2);
		if(resourceCapable){
			gameObject.FindGameObjectWithTag("GameManager").GetComponent(GameManager).addResourceBuilding(this);
			for(var i=0;i<resourcesNamesAllowed.length;i++){
				resourcesAllowed.Add(resourcesNamesAllowed[i], 0);
			}
		}
	}
	
	//Update the array of spawn points in the Selectable layer
	function updatePossibleSpawnPoints(unitWidth:float){
		possibleSpawnPointsUnits.Clear();
		for(var i=0;i<possibleSpawnPointsObstacles.Count;i++){
			//If sphere collided with nothing
			//Then valid point
			if(!Physics.CheckSphere(possibleSpawnPointsObstacles[i], unitWidth/2, 1<<LayerConstants.OBJECTS)){
				possibleSpawnPointsUnits.Add(possibleSpawnPointsObstacles[i]);
			}
		}
	}
	
	//Find points that are valid in Obstacle array
	function findPossibleSpawnPoints(unitWidth:float){
		//Number of points in x Direction
		var xDivisions:int = (collider.bounds.size.x/unitWidth)+2;
		//Number of points in z Direction
		var yDivisions:int = (collider.bounds.size.z/unitWidth)+2;
		//Position of Building
		var pos:Vector3 = transform.position;
		//Bounds of Building
		var bounds:Vector3 = collider.bounds.size;
		//Leftmost x value of points
		var leftBound:float = pos.x-(bounds.x/2)-(unitWidth/2);
		//Rightmost x value of points
		var rightBound:float = pos.x+(bounds.x/2)+(unitWidth/2);
		//Bottommost z value of points
		var bottomBound:float = pos.z-(bounds.z/2)-(unitWidth/2);
		//Topmost z value of points
		var topBound:float = pos.z+(bounds.z/2)+(unitWidth/2);
		//The spawn point found
		var point:Vector3;
		//Clear current Spawn Points against Obstacle
		possibleSpawnPointsObstacles.Clear();
		
		//Check spawn points in a box style
		//First check x direction
		//Then check z direction
		for(var i=0;i<xDivisions;i++){
			point = Vector3(leftBound+(i*unitWidth), pos.y-(bounds.y/2), bottomBound -.25);
			if(!Physics.CheckSphere(point, unitWidth/2, 1<<LayerConstants.OBSTACLES)){
				possibleSpawnPointsObstacles.Add(point);
			}
			point = Vector3(leftBound+(i*unitWidth), pos.y-(bounds.y/2), topBound +.25);
			if(!Physics.CheckSphere(point, unitWidth/2, 1<<LayerConstants.OBSTACLES)){
				possibleSpawnPointsObstacles.Add(point);
			}
		}
		for(i=0;i<yDivisions;i++){
			point = Vector3(leftBound -.25, pos.y-(bounds.y/2), bottomBound+(i*unitWidth));
			if(!Physics.CheckSphere(point, unitWidth/2, 1<<LayerConstants.OBSTACLES)){
				possibleSpawnPointsObstacles.Add(point);
			}
			point = Vector3(rightBound +.25, pos.y-(bounds.y/2), bottomBound+(i*unitWidth));
			if(!Physics.CheckSphere(point, unitWidth/2, 1<<LayerConstants.OBSTACLES)){
				possibleSpawnPointsObstacles.Add(point);
			}
		}
	}
	
	//Find the closest spawn point to the waypoint
	function findClosestSpawnPoint(unitWidth:float):Vector3{
		//Get latest list of possible spawn points
		updatePossibleSpawnPoints(unitWidth);
		//Convert positions to 2D coordinates
		var pos2D:Vector2 = Vector2(transform.position.x,transform.position.z);
		if(GetComponent(RTSObject).commands.Count > 0){
			var point:Vector3 = GetComponent(RTSObject).waypoint.positions[1];
			var wayPoint2D:Vector2 = Vector2(point.x, point.z);
		}
		else{
			wayPoint2D = Vector2(defaultSpawnPoint.x, defaultSpawnPoint.z);
		}
		var spawnLocation2D:Vector2;
		var spawnLocation:Vector3;
		for(var i=0;i<possibleSpawnPointsUnits.Count;i++){
			var possiblePoint2D:Vector2 = Vector2(possibleSpawnPointsUnits[i].x, possibleSpawnPointsUnits[i].z);
			//Calculate the Vectors from Building to Waypoint, Building to Point, and Building to Spawn Location
			var wayPoint2Building = wayPoint2D-pos2D;
			var possiblePoint2Building = possiblePoint2D-pos2D;
			var spawnLocation2Building = spawnLocation2D-pos2D;
			//If there has been atleast one spawn point found
			if(spawnLocation != Vector3.zero){
				//If angle is less
				if(Vector2.Angle(wayPoint2Building, possiblePoint2Building) < Vector2.Angle(wayPoint2Building, spawnLocation2Building)){
					//Store Spawn Location
					spawnLocation = possibleSpawnPointsUnits[i];
					spawnLocation2D = Vector2(possibleSpawnPointsUnits[i].x, possibleSpawnPointsUnits[i].z);
				}
			}
			//Else there is no spawn point yet
			else{
				//Check angle against 180
				if(Vector2.Angle(wayPoint2Building, possiblePoint2Building) < 180){
					spawnLocation = possibleSpawnPointsUnits[i];
					spawnLocation2D = Vector2(possibleSpawnPointsUnits[i].x, possibleSpawnPointsUnits[i].z);
				}
			}
		}
		return spawnLocation;
	}
	
	//Update whether the Building should create a Unit
	function updateBuildTime(){
		//If there is a Unit qued
		if(currentQue > 0){
			//Increment time
			currentBuildTime += Time.deltaTime;
			//If amount of build time exceeds time needed to build Unit
			if(currentBuildTime >= buildQue[0].GetComponent(RTSObject).buildTime){
				createUnit(buildQue[0]);
				//Remove first qued Unit
				buildQue.RemoveAt(0);
				currentQue--;
				currentBuildTime = 0;
			}
		}
	}
	
	function resourcesAvailable(unit:GameObject):boolean{
		var player:RTSPlayer = GameObject.FindGameObjectWithTag("Player").GetComponent(RTSPlayer);
		var names:String[] = unit.transform.GetComponent(RTSObject).resourceNames;
		var amounts:int[] = unit.transform.GetComponent(RTSObject).resourceAmounts;
		var currentResource:int;
		for(var i=0;i<unit.transform.GetComponent(RTSObject).resourceNames.length;i++){
			currentResource = player.resources[names[i]];
			if(currentResource < amounts[i]){
				return false;
			}
		}
		for(i=0;i<unit.transform.GetComponent(RTSObject).resourceNames.length;i++){
			currentResource = player.resources[names[i]];
			player.resources[names[i]] = currentResource - amounts[i];
		}
		return true;
	}
	
	//Add Unit to Build Que
	function addUnittoBuildQue(unit:GameObject){
		if(!resourcesAvailable(unit)){
			Debug.Log("Insufficient Funds");
			return;
		}
		//If amount of qued Units is less than maximum que amount
		if(currentQue < queMax){
			//Add Unit to que
			buildQue.Add(unit);
			currentQue++;
		}
		//Else build que is full
		else{
			Debug.Log("Build que is full");
			return;
		}
	}
	
	//Create Unit at spawn point
	function createUnit(unit:GameObject){
		var tempUnit:GameObject;
		var pos:Vector3 = findClosestSpawnPoint(4);
		//If pos is (0,0,0)
		//Can't spawn Unit
		if(pos == Vector3.zero){
			Debug.Log("Unit could not be created");
			return;
		}
		//Instantiate Unit at point
		tempUnit = Instantiate(unit, pos, Quaternion(0,0,0,0));
		//Set team and alliance
		tempUnit.transform.GetComponent(RTSObject).team = team;
		tempUnit.transform.GetComponent(RTSObject).alliance = alliance;
		//Wait for Move component to become available
		while(tempUnit.GetComponent(Move) == null || tempUnit.GetComponent(Stop) == null){
			yield WaitForEndOfFrame();
		}
		//Move Unit to Waypoint
		if(GetComponent(RTSObject).commands.Count > 0){
			tempUnit.GetComponent(RTSObject).commands = new List.<Command>(GetComponent(RTSObject).commands);
			/*************Figure out how to start the unit towards the command when spawned**************/
		}
		else{
			tempUnit.GetComponent(Movable).Move(defaultSpawnPoint, false);
		}
	}
	
	function Update(){
		updateBuildTime();
		/*if(possibleSpawnPointsObstacles != null){
			for(var i=0;i<possibleSpawnPointsObstacles.length;i++){
				Debug.DrawRay(possibleSpawnPointsObstacles[i], Vector3(0,10,0), Color.blue);
			}
		}
		if(possibleSpawnPointsUnits != null){
			for(i=0;i<possibleSpawnPointsUnits.length;i++){
				Debug.DrawRay(possibleSpawnPointsUnits[i], Vector3(0,10,0), Color.green);
			}
		}*/
	}
}