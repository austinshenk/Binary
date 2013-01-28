#pragma strict

public class MovableNeutral extends RTSObject implements Movable{

	public var busy:boolean = false;
	public var speedLinear:int;
	private var movingSpeedLinear:int;
	private var waitingSpeedLinear:int = 5;
	private var moveDir:Vector3;
	public var speedAngular:int;
	private var startRotation:boolean = false;
	
	private var seeker:Seeker;
	private var flocking:boolean = false;
	private var commandPoint:Vector3;       
	private var centerOffset:Vector3;
	private var path:Pathfinding.Path;
	private var nextWaypointDistance:float = 3;
	private var currentWaypoint:int = 0;
	private var waypoint:Vector3;
	public var reachedDestination:boolean = true;
	
	private var tempMoveDir:Vector3;
	private var hit:RaycastHit;       
	
	function Start(){
		super.Start();
		movingSpeedLinear = speedLinear;
		seeker = gameObject.AddComponent(Seeker);
	}
	
	function OnCollisionStay(col:Collision){
		if(col.transform.GetComponent(RTSObject) == null){return;}
		if(col.transform.GetComponent(Building) != null){return;}
		//Unit
		var dir:Vector3;
		var colPriority:float = col.transform.GetComponent(RTSObject).priority;
		if(team == col.transform.GetComponent(RTSObject).team){
			if(priority < colPriority){
				dir = Vector3.Normalize(col.transform.position-transform.position);
				simpleMove(dir);
			}
			if(priority == colPriority){
				if(priority == 0){
					dir = Vector3.Normalize(col.transform.position-transform.position);
					col.transform.GetComponent(MovableNeutral).simpleMove(dir);
				}
				else if(priority == 1){
					if(Vector3.Angle((col.transform.position-transform.position), transform.TransformDirection(Vector3(0,0,1))) < 30){
						Wait();
					}
				}
			}
			//Idle & Idle
			/*if(priority == 0 && colPriority == 0){
				dir = Vector3.Normalize(col.transform.position-transform.position);
				col.transform.GetComponent(MovableNeutral).simpleMove(dir);
			}
			//Moving & Moving & Command point is the same
			if((priority == 1 && colPriority == 1) && commandPoint == col.transform.GetComponent(MovableNeutral).commandPoint){
				if(Vector3.Angle((col.transform.position-transform.position), transform.TransformDirection(Vector3(0,0,1))) < 30){
					Wait();
				}
			}
			//Moving & Attacking
			//Still debugging this because Units do not move around Attacking Units
			if(priority == 1 && colPriority == 2){
				dir = transform.TransformDirection(Vector3(0,0,-1));
				simpleMove(dir);
			}*/
			//General - Move collider out of the way because its Priority is less
			if(priority > colPriority){
				//If the colliding Unit has Reached Destination and
				//    Units are told to Flock then Stop this Unit
				if(col.transform.GetComponent(MovableNeutral).reachedDestination && flocking){
					reachedDestination = true;
					Stop();
				}
				//Otherwise they are moving in a Relative Center motion
				else{
					dir = Vector3.Normalize(col.transform.position-transform.position);
					col.transform.GetComponent(MovableNeutral).simpleMove(dir);
				}
			}
		}
		else{
			dir = transform.TransformDirection(Vector3(0,0,-1));
			simpleMove(dir);
		}
	}
	
	function OnCollisionExit(){
		Begin();
	}
	
	function setCommandPointOffset(point:Vector3){
		centerOffset = point;
	}
	
	function setFlocking(f:boolean){
		flocking = f;
	}
	
	function simpleMove(dir:Vector3){
		moveDir = dir;
	}
	
	function Begin(){
		speedLinear = movingSpeedLinear;
	}
	function Wait(){
		speedLinear = waitingSpeedLinear;
	}

	function Move(point:Vector3, interruptable:boolean){
		commandPoint = point;
		reachedDestination = false;
		if(interruptable){
			//Allow Attacking to Interrupt
		}
		else{
			//End Attacking
		}
		if(!flocking){
			point += centerOffset;
		}
		if(!Physics.Linecast(transform.position, point, 1<<LayerConstants.OBSTACLES)){
			path = new Pathfinding.Path();
			path.vectorPath = [point];
			currentWaypoint = 0;
		}
		else{
			seeker.StartPath(transform.position, point, pathCalculationComplete);
		}
	}
	
	function pathCalculationComplete(p:Pathfinding.Path) {
		if(p.error){return;}
		var end = p.vectorPath[p.vectorPath.Length-1];
    	end.y += .25;
    	var checkPoint:Vector3 = commandPoint;
    	checkPoint.y += .25;
    	if(end.y >= checkPoint.y){
    		checkPoint.y = end.y;
    	}
    	else{
    		end.y = checkPoint.y;
    	}
		if(Physics.Linecast(checkPoint, end, 1<<LayerConstants.OBSTACLES)){
    		seeker.StartPath(transform.position, commandPoint, pathCalculationComplete);
    	}
    	else{
	        path = p;
	        currentWaypoint = 0;
	        startRotation = true;
	    }
	}
	
	function Stop(){
		priority = 0;
		reachedDestination = true;
	}
	
	function Rotate(point:Vector3):boolean{
		var initRotation = transform.rotation;
		point.y = transform.position.y;
		var lookRotation = Quaternion.FromToRotation(Vector3(0,0,1), Vector3.Normalize(point-transform.position));
		transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, speedAngular*Time.deltaTime);
		if(transform.rotation == initRotation){
			return true;
		}
		return false;
	}
	
	function FixedUpdate() {
		if(!collider.enabled) return;
		if(Physics.Raycast(transform.position+Vector3(0,5,0), Vector3(0,-1,0), hit, Mathf.Infinity, 1<<LayerConstants.GROUND)){
			//Set Unit Y positon to hit point + half the colliders Z bounds + buffer
			rigidbody.position.y = hit.point.y + height + .25;
		}
		if(moveDir.magnitude != 0){                           //If Unit is being told to Move
			                                              //Then Move
			tempMoveDir = moveDir*speedLinear*Time.deltaTime;
			//Cast Ray in front to Obstacles Layer
			if(Physics.Raycast(transform.position, moveDir, hit, collider.bounds.size.z/1.5, 1<<LayerConstants.OBSTACLES)){
			    //reverse direction and clear Path
				tempMoveDir *= -1;
				if(path != null){
					currentWaypoint = path.vectorPath.Length;
				}
				else{
					Stop();
				}
			}
			//Minimum Movement Speed
			if(tempMoveDir.magnitude < .0125){
				tempMoveDir *= 0;
			}
			//Move Rigidbody so collisions occur
			rigidbody.MovePosition(rigidbody.position+tempMoveDir);
			//Clear pos
			moveDir *= 0;
		}
		
	}
	
	function Update(){
		if(path == null){return;}
	    //End of path
	    if(currentWaypoint >= path.vectorPath.Length) {Stop();return;}
	    //Priority set to 1 for Moving
	    GetComponent(RTSObject).priority = 1;
	    //Rotate Unit to initial point and afterwards use Look at
	    if(startRotation){
	    	if(Rotate(path.vectorPath[1])){
	    		startRotation = false;
	    	}
	    	else{
	    		return;
	    	}
	    }
	    waypoint = path.vectorPath[currentWaypoint];
	    transform.LookAt(Vector3(waypoint.x, transform.position.y, waypoint.z));
	    simpleMove(Vector3.Normalize(waypoint-transform.position));
	    if(Vector2.Distance(Vector2(transform.position.x,transform.position.z),Vector2(waypoint.x,waypoint.z)) < nextWaypointDistance) {
	        currentWaypoint++;
	        return;
	   	}
   	}


}