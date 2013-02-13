#pragma strict

public class UnitManager{
	static function sendUnitInfo(units:List.<RTSObject>, hit:RaycastHit, multiCommand:boolean){
		var center:Vector3;
		for(var i:int=0;i<units.Count;i++){
			var temp:RTSObject = units[i];
			center += temp.transform.position;
		}
		center = center/units.Count;
		for(i=0;i<units.Count;i++){
			temp = units[i];
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