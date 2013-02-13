#pragma strict

public class Move extends Action{
	public function Begin(params:Object[]){
		var hit:RaycastHit = params[0];
		var interrupt:boolean = params[1];
		GetComponent(Movable).Move(hit.point, interrupt);
	}
	public function Ended():boolean{
		if(GetComponent(Movable) != null)
		return GetComponent(Movable).Ended();
		return true;
	}
}