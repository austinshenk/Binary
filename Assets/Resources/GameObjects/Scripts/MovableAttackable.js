#pragma strict

public class MovableAttackable extends MovableNeutral implements Attackable{
	public var damage:int;
	public var range:float = 0;
	public var duration:float = 0;  
	function Attack(g:Damagable){
		g.Damage(damage);
	}
}