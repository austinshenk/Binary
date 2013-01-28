#pragma strict

public class UnitManager{
	static function sendUnitInfo(units:Array, hit:RaycastHit, multiCommand:boolean){
		var center:Vector3;
		for(var i:int=0;i<units.length;i++){
			var temp:RTSObject = units[i] as RTSObject;
			center += temp.transform.position;
		}
		center = center/units.length;
		for(i=0;i<units.length;i++){
			temp = units[i] as RTSObject;
			if(temp.GetComponent(Movable) != null){
				temp.GetComponent(Movable).setCommandPointOffset(temp.transform.position-center);
				var flock:boolean = (Vector3.Distance(center, hit.point) < Vector3.Distance(temp.transform.position, center));
				temp.GetComponent(Movable).setFlocking(flock);
			}
			if(multiCommand){
				temp.addCommand(hit);
			}
			else{
				temp.setCommand(hit);
			}
		}
	}
}